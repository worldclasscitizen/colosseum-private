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

        public enum StatusEffect { None, Freeze, Burn, Poison }
        [Networked] public StatusEffect AppliedStatus { get; set; } = StatusEffect.None;

        [Networked] private TickTimer _lifetimeTimer { get; set; }
        [Networked] private TickTimer _ownerIgnoreTimer { get; set; }
        [Networked] private int _remainingBounces { get; set; }
        [Networked] private PlayerRef _owner { get; set; }

        private Rigidbody2D _rb;
        private bool _ownerIgnored = false;

        public void Init(PlayerRef owner, Vector2 direction)
        {
            _owner = owner;
            _remainingBounces = BounceCount;

            // 다른 총알들과 충돌 무시
            var myCollider = GetComponent<Collider2D>();
            var allBullets = FindObjectsOfType<BulletController>();
            foreach (var other in allBullets)
            {
                if (other == this) continue;
                var otherCollider = other.GetComponent<Collider2D>();
                if (otherCollider != null && myCollider != null)
                    Physics2D.IgnoreCollision(myCollider, otherCollider);
            }

            _rb = GetComponent<Rigidbody2D>();
            _rb.gravityScale = _gravityScale;
            _rb.drag = _linearDrag;

            // 바운스가 있으면 물리 재질 설정
            if (BounceCount > 0 && myCollider != null)
            {
                var bounceMat = new PhysicsMaterial2D("BulletBounce");
                bounceMat.bounciness = 1f;  // 완전 탄성 반사
                bounceMat.friction = 0f;
                myCollider.sharedMaterial = bounceMat;
            }

            transform.localScale = Vector3.one * 0.08f * SizeMultiplier;

            float speed = _baseSpeed * SpeedMultiplier;
            _rb.velocity = direction.normalized * speed;

            Debug.Log($"[Colosseum] Bullet Init - Speed:{speed} (base:{_baseSpeed} x {SpeedMultiplier}), " +
                $"Size:{SizeMultiplier}, Bounce:{BounceCount}, Damage:{DamageMultiplier}");

            _lifetimeTimer = TickTimer.CreateFromSeconds(Runner, _lifetime);
            _ownerIgnoreTimer = TickTimer.CreateFromSeconds(Runner, _ownerIgnoreTime);

            // 발사 직후 소유자와 충돌 무시 (총구에서 몸 통과)
            SetOwnerCollisionIgnore(true);
        }

        public override void FixedUpdateNetwork()
        {
            if (_lifetimeTimer.Expired(Runner))
            {
                Runner.Despawn(Object);
                return;
            }

            // 일정 시간 후 소유자 충돌 다시 활성화 (자해 가능)
            if (_ownerIgnored && _ownerIgnoreTimer.Expired(Runner))
            {
                SetOwnerCollisionIgnore(false);
            }
        }

        private void SetOwnerCollisionIgnore(bool ignore)
        {
            _ownerIgnored = ignore;
            var bulletCollider = GetComponent<Collider2D>();
            if (bulletCollider == null) return;

            var players = FindObjectsOfType<NetworkObject>();
            foreach (var p in players)
            {
                if (p.InputAuthority == _owner)
                {
                    var playerColliders = p.GetComponentsInChildren<Collider2D>();
                    foreach (var pc in playerColliders)
                    {
                        Physics2D.IgnoreCollision(bulletCollider, pc, ignore);
                    }
                    break;
                }
            }
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (!Object.HasStateAuthority) return;

            // 총알끼리는 충돌 무시
            if (collision.gameObject.GetComponent<BulletController>() != null)
                return;

            // 플레이어에 맞으면 데미지 주고 소멸 (바운스 관계없이)
            var health = collision.gameObject.GetComponent<PlayerHealth>();
            if (health != null)
            {
                float finalDamage = _baseDamage * DamageMultiplier;
                health.TakeDamage(finalDamage, _owner, AppliedStatus);
                Runner.Despawn(Object);
                return;
            }

            // 벽/바닥에 부딪혔을 때
            if (_remainingBounces > 0)
            {
                _remainingBounces--;
                Debug.Log($"[Colosseum] Bullet bounced! Remaining: {_remainingBounces}");
                // PhysicsMaterial2D의 bounciness가 물리 반사를 처리함
            }
            else
            {
                Runner.Despawn(Object);
            }
        }

        public float GetDamage()
        {
            return _baseDamage * DamageMultiplier;
        }
    }
}
