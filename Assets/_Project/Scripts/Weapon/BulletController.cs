using UnityEngine;
using Fusion;
using Colosseum.Player;

namespace Colosseum.Weapon
{
    public class BulletController : NetworkBehaviour
    {
        [Header("Base Stats")]
        [SerializeField] private float _baseDamage = 10f;
        [SerializeField] private float _baseSpeed = 25f;
        [SerializeField] private float _gravityScale = 0.5f;
        [SerializeField] private float _linearDrag = 0.2f;
        [SerializeField] private float _lifetime = 5f;
        [SerializeField] private float _ownerIgnoreTime = 0.3f;

        [Networked] public float DamageMultiplier { get; set; } = 1f;
        [Networked] public float SpeedMultiplier { get; set; } = 1f;
        [Networked] public float SizeMultiplier { get; set; } = 1f;
        [Networked] public int BounceCount { get; set; } = 0;
        [Networked] public int PierceCount { get; set; } = 0;
        [Networked] public float KnockbackForce { get; set; } = 0f;
        [Networked] public float ExplosionRadius { get; set; } = 0f;
        [Networked] public float ExplosionDamageRatio { get; set; } = 0.5f;

        public enum StatusEffect { None, Freeze, Burn, Poison }
        [Networked] public StatusEffect AppliedStatus { get; set; } = StatusEffect.None;

        [Networked] private TickTimer _lifetimeTimer { get; set; }
        [Networked] private TickTimer _ownerIgnoreTimer { get; set; }
        [Networked] private int _remainingBounces { get; set; }
        [Networked] private int _remainingPierces { get; set; }
        [Networked] private PlayerRef _owner { get; set; }

        private Rigidbody2D _rb;
        private Collider2D _physicsCollider;
        private bool _ownerIgnoreExpired = false;

        public void Init(PlayerRef owner, Vector2 direction)
        {
            _owner = owner;
            _remainingBounces = BounceCount;
            _remainingPierces = PierceCount;

            _physicsCollider = GetComponent<Collider2D>();
            _rb = GetComponent<Rigidbody2D>();
            _rb.gravityScale = _gravityScale;
            _rb.drag = _linearDrag;

            // ── 다른 총알과 물리 충돌 무시 ──
            var allBullets = FindObjectsOfType<BulletController>();
            foreach (var other in allBullets)
            {
                if (other == this) continue;
                var otherCollider = other.GetComponent<Collider2D>();
                if (otherCollider != null && _physicsCollider != null)
                    Physics2D.IgnoreCollision(_physicsCollider, otherCollider);
            }

            // ── 모든 플레이어와 물리 충돌 무시 (밀림 방지) ──
            // 플레이어 히트 감지는 트리거(OnTriggerEnter2D)로 처리
            IgnoreAllPlayerPhysics();

            // ── 플레이어 히트 감지용 트리거 콜라이더 추가 ──
            CreateHitTrigger();

            // 바운스 물리 재질
            if (BounceCount > 0 && _physicsCollider != null)
            {
                var bounceMat = new PhysicsMaterial2D("BulletBounce");
                bounceMat.bounciness = 1f;
                bounceMat.friction = 0f;
                _physicsCollider.sharedMaterial = bounceMat;
            }

            transform.localScale = Vector3.one * 0.08f * SizeMultiplier;

            float speed = _baseSpeed * SpeedMultiplier;
            _rb.velocity = direction.normalized * speed;

            _lifetimeTimer = TickTimer.CreateFromSeconds(Runner, _lifetime);
            _ownerIgnoreTimer = TickTimer.CreateFromSeconds(Runner, _ownerIgnoreTime);

            Debug.Log($"[Colosseum] Bullet Init - Speed:{speed}, Size:{SizeMultiplier}, " +
                $"Bounce:{BounceCount}, Pierce:{PierceCount}, Damage:{DamageMultiplier}");
        }

        public override void FixedUpdateNetwork()
        {
            if (_lifetimeTimer.Expired(Runner))
            {
                Runner.Despawn(Object);
                return;
            }

            // 일정 시간 후 소유자에게도 히트 판정 활성화 (자해 가능)
            if (!_ownerIgnoreExpired && _ownerIgnoreTimer.Expired(Runner))
            {
                _ownerIgnoreExpired = true;
            }
        }

        // ──────────────────────────────────────────────
        //  플레이어와의 물리 충돌을 전부 무시
        //  → 총알이 플레이어를 물리적으로 밀지 않음
        // ──────────────────────────────────────────────
        private void IgnoreAllPlayerPhysics()
        {
            if (_physicsCollider == null) return;
            var players = FindObjectsOfType<PlayerHealth>();
            foreach (var p in players)
            {
                var playerColliders = p.GetComponentsInChildren<Collider2D>();
                foreach (var pc in playerColliders)
                {
                    Physics2D.IgnoreCollision(_physicsCollider, pc, true);
                }
            }
        }

        // ──────────────────────────────────────────────
        //  히트 감지용 트리거 콜라이더 (자식 오브젝트)
        //  물리 콜라이더와 별개로, 플레이어 겹침만 감지
        // ──────────────────────────────────────────────
        private void CreateHitTrigger()
        {
            var triggerObj = new GameObject("HitTrigger");
            triggerObj.transform.SetParent(transform, false);
            triggerObj.transform.localPosition = Vector3.zero;
            triggerObj.layer = gameObject.layer;

            // 물리 콜라이더와 같은 크기의 트리거
            var triggerCollider = triggerObj.AddComponent<CircleCollider2D>();
            var originalCircle = _physicsCollider as CircleCollider2D;
            triggerCollider.radius = originalCircle != null ? originalCircle.radius : 0.5f;
            triggerCollider.isTrigger = true;

            // 트리거 이벤트를 부모(이 스크립트)로 전달하는 릴레이
            var relay = triggerObj.AddComponent<TriggerRelay>();
            relay.Init(this);
        }

        // ──────────────────────────────────────────────
        //  트리거 히트 처리 (플레이어 전용)
        //  물리 충돌이 아니라 겹침 감지이므로 밀림 없음
        //  넉백 카드가 있을 때만 AddForce로 밀어냄
        // ──────────────────────────────────────────────
        public void OnHitTrigger(Collider2D other)
        {
            if (!Object.HasStateAuthority) return;

            var health = other.GetComponent<PlayerHealth>();
            if (health == null) return;

            // 소유자 무시 시간 내에는 히트 무시
            var netObj = other.GetComponent<NetworkObject>();
            if (netObj != null && netObj.InputAuthority == _owner && !_ownerIgnoreExpired)
                return;

            // 데미지 적용
            float finalDamage = _baseDamage * DamageMultiplier;
            health.TakeDamage(finalDamage, _owner, AppliedStatus);

            // 넉백 (Shove 카드가 있을 때만)
            if (KnockbackForce > 0f)
            {
                var targetRb = other.GetComponent<Rigidbody2D>();
                if (targetRb != null)
                {
                    Vector2 knockDir = _rb.velocity.normalized;
                    targetRb.AddForce(knockDir * KnockbackForce, ForceMode2D.Impulse);
                    Debug.Log($"[Colosseum] Knockback applied: {KnockbackForce} force");
                }
            }

            // 플레이어에게 맞으면 항상 소멸 (관통은 벽 전용)
            Explode(other);
            Runner.Despawn(Object);
        }

        // ──────────────────────────────────────────────
        //  벽/바닥 충돌 (물리 콜라이더)
        //  바운스 → 관통(벽 통과) → 소멸 순서로 처리
        // ──────────────────────────────────────────────
        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (!Object.HasStateAuthority) return;

            // 총알끼리는 무시
            if (collision.gameObject.GetComponent<BulletController>() != null)
                return;

            // 바운스가 남아있으면 반사
            if (_remainingBounces > 0)
            {
                _remainingBounces--;
                Debug.Log($"[Colosseum] Bullet bounced! Remaining: {_remainingBounces}");
                return;
            }

            // 관통이 남아있으면 벽을 통과
            if (_remainingPierces > 0)
            {
                _remainingPierces--;
                // 이 벽과의 충돌을 무시하여 총알이 통과
                if (_physicsCollider != null && collision.collider != null)
                {
                    Physics2D.IgnoreCollision(_physicsCollider, collision.collider, true);
                }
                Debug.Log($"[Colosseum] Bullet pierced wall! Remaining: {_remainingPierces}");
                return;
            }

            // 바운스도 관통도 없으면 소멸
            Explode(null);
            Runner.Despawn(Object);
        }

        /// <summary>
        /// 폭발: 충돌 지점 주변 반경 내 플레이어에게 범위 데미지.
        /// directHitCollider가 있으면 해당 대상은 스킵 (이미 풀 데미지를 받음).
        /// </summary>
        private void Explode(Collider2D directHitCollider)
        {
            if (ExplosionRadius <= 0f) return;

            float splashDamage = _baseDamage * DamageMultiplier * ExplosionDamageRatio;
            var hits = Physics2D.OverlapCircleAll(transform.position, ExplosionRadius);

            foreach (var hit in hits)
            {
                if (hit == directHitCollider) continue;

                var hitHealth = hit.GetComponent<PlayerHealth>();
                if (hitHealth != null)
                {
                    hitHealth.TakeDamage(splashDamage, _owner, StatusEffect.None);

                    // 폭발 넉백 (넉백 카드가 있을 때만)
                    if (KnockbackForce > 0f)
                    {
                        var hitRb = hit.GetComponent<Rigidbody2D>();
                        if (hitRb != null)
                        {
                            Vector2 blastDir = ((Vector2)hit.transform.position - (Vector2)transform.position).normalized;
                            hitRb.AddForce(blastDir * KnockbackForce * 0.5f, ForceMode2D.Impulse);
                        }
                    }

                    Debug.Log($"[Colosseum] Explosion hit {hit.name} for {splashDamage} splash damage");
                }
            }
        }

        public float GetDamage()
        {
            return _baseDamage * DamageMultiplier;
        }
    }

    /// <summary>
    /// 자식 트리거 오브젝트의 OnTriggerEnter2D를
    /// 부모 BulletController.OnHitTrigger()로 전달하는 릴레이.
    /// </summary>
    public class TriggerRelay : MonoBehaviour
    {
        private BulletController _bullet;

        public void Init(BulletController bullet)
        {
            _bullet = bullet;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_bullet != null)
                _bullet.OnHitTrigger(other);
        }
    }
}
