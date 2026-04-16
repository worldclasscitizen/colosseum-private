using UnityEngine;
using Fusion;
using Colosseum.Network;

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

        public override void FixedUpdateNetwork()
        {
            if (GetInput(out NetworkInputData input))
            {
                if (input.IsFirePressed && _fireCooldown.ExpiredOrNotRunning(Runner))
                {
                    Fire(input.AimDirection);
                    float cooldown = _fireRate / FireRateMultiplier;
                    _fireCooldown = TickTimer.CreateFromSeconds(Runner, cooldown);
                }
            }
        }

        private void Fire(Vector2 aimDirection)
        {
            if (!Object.HasStateAuthority) return;

            // 조준 방향 계산 (마우스 위치 - 총구 위치)
            Vector2 dir = aimDirection - (Vector2)_muzzle.position;
            if (dir == Vector2.zero) dir = Vector2.right;
            dir = dir.normalized;

            // 기본 총알 발사
            SpawnBullet(dir);

            // 카드로 추가 총알이 있으면 약간 각도 틀어서 발사
            for (int i = 0; i < ExtraBullets; i++)
            {
                float angle = (i + 1) * 10f * (i % 2 == 0 ? 1f : -1f);
                Vector2 spreadDir = Rotate(dir, angle);
                SpawnBullet(spreadDir);
            }
        }

        private void SpawnBullet(Vector2 direction)
        {
            NetworkObject bulletObj = Runner.Spawn(
                _bulletPrefab,
                _muzzle.position,
                Quaternion.identity,
                Object.InputAuthority);

            var bullet = bulletObj.GetComponent<BulletController>();
            if (bullet != null)
            {
                bullet.Init(Object.InputAuthority, direction);
            }
        }

        private Vector2 Rotate(Vector2 v, float degrees)
        {
            float rad = degrees * Mathf.Deg2Rad;
            float cos = Mathf.Cos(rad);
            float sin = Mathf.Sin(rad);
            return new Vector2(v.x * cos - v.y * sin, v.x * sin + v.y * cos);
        }
    }
}
