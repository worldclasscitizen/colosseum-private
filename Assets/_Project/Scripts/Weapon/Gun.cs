using UnityEngine;
using Fusion;
using Colosseum.Network;
using Colosseum.Card;

namespace Colosseum.Weapon
{
    public class Gun : NetworkBehaviour
    {
        [Header("Fire Settings")]
        [SerializeField] private float _fireRate = 0.3f;
        [SerializeField] private NetworkPrefabRef _bulletPrefab;
        [SerializeField] private Transform _muzzle;

        [Networked] public float FireRateMultiplier { get; set; } = 1f;
        [Networked] public int ExtraBullets { get; set; } = 0;
        [Networked] private TickTimer _fireCooldown { get; set; }
        [Networked] private Vector2 _networkedAimDir { get; set; }

        private CardEffect _cardEffect;

        public override void Spawned()
        {
            // 부모(Player)에서 CardEffect 컴포넌트 찾기
            _cardEffect = GetComponentInParent<CardEffect>();
        }

        public override void FixedUpdateNetwork()
        {
            if (GetInput(out NetworkInputData input))
            {
                // 마우스 월드 좌표 → 총 위치 기준 방향 계산
                Vector2 aimDir = (Vector2)input.AimDirection - (Vector2)transform.position;
                if (aimDir == Vector2.zero) aimDir = Vector2.right;
                _networkedAimDir = aimDir.normalized;

                if (input.IsFirePressed && _fireCooldown.ExpiredOrNotRunning(Runner))
                {
                    Fire();
                    float cooldown = _fireRate / FireRateMultiplier;
                    _fireCooldown = TickTimer.CreateFromSeconds(Runner, cooldown);
                }
            }
        }

        public override void Render()
        {
            if (_networkedAimDir != Vector2.zero)
            {
                float angle = Mathf.Atan2(_networkedAimDir.y, _networkedAimDir.x) * Mathf.Rad2Deg;
                transform.localRotation = Quaternion.Euler(0f, 0f, angle);
            }
        }

        private void Fire()
        {
            if (!Object.HasStateAuthority) return;

            Vector2 fireDir = _networkedAimDir;
            SpawnBullet(fireDir);

            for (int i = 0; i < ExtraBullets; i++)
            {
                float angle = (i + 1) * 10f * (i % 2 == 0 ? 1f : -1f);
                Vector2 spreadDir = Rotate(fireDir, angle);
                SpawnBullet(spreadDir);
            }
        }

        private void SpawnBullet(Vector2 direction)
        {
            NetworkObject bulletObj = Runner.Spawn(
                _bulletPrefab,
                _muzzle.position,
                Quaternion.identity,
                Object.InputAuthority
            );

            var bullet = bulletObj.GetComponent<BulletController>();
            if (bullet != null)
            {
                // CardEffect 강화 수치를 총알에 적용
                if (_cardEffect != null)
                {
                    Debug.Log($"[Colosseum] Gun reading CardEffect - SizeMul:{_cardEffect.BulletSizeMultiplier}, SpeedMul:{_cardEffect.BulletSpeedMultiplier}, Bounce:{_cardEffect.BonusBounce}");
                    bullet.DamageMultiplier = _cardEffect.DamageMultiplier;
                    bullet.SpeedMultiplier = _cardEffect.BulletSpeedMultiplier;
                    bullet.SizeMultiplier = _cardEffect.BulletSizeMultiplier;
                    bullet.BounceCount = _cardEffect.BonusBounce;

                    if (_cardEffect.CurrentStatusEffect != 0)
                    {
                        bullet.AppliedStatus = (BulletController.StatusEffect)_cardEffect.CurrentStatusEffect;
                    }
                }

                bullet.Init(Object.InputAuthority, direction);
            }
            else
            {
                Debug.LogWarning("[Colosseum] Gun: _cardEffect is NULL! Card effects won't apply.");
                bullet.Init(Object.InputAuthority, direction);
            }
        }

        private Vector2 Rotate(Vector2 v, float degrees)
        {
            float rad = degrees * Mathf.Deg2Rad;
            float cos = Mathf.Cos(rad);
            float sin = Mathf.Sin(rad);
            return new Vector2(
                v.x * cos - v.y * sin,
                v.x * sin + v.y * cos
            );
        }
    }
}
