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

        [Header("Magazine Settings")]
        [SerializeField] private int _magazineSize = 1;
        [SerializeField] private float _reloadTime = 1.5f;

        [Networked] public float FireRateMultiplier { get; set; } = 1f;
        [Networked] public int ExtraBullets { get; set; } = 0;
        [Networked] private TickTimer _fireCooldown { get; set; }
        [Networked] private Vector2 _networkedAimDir { get; set; }

        // 탄창 상태
        [Networked] public int CurrentAmmo { get; set; }
        [Networked] private TickTimer _reloadTimer { get; set; }
        [Networked] public NetworkBool IsReloading { get; set; }
        [Networked] public float ReloadDuration { get; set; }

        // 과충전: 재장전 직후 첫 발 데미지 증폭
        [Networked] public NetworkBool IsOvercharged { get; set; }

        private CardEffect _cardEffect;

        /// <summary>
        /// 재장전 진행률 (0 → 1). UI에서 참조.
        /// </summary>
        public float ReloadProgress
        {
            get
            {
                if (!IsReloading || ReloadDuration <= 0f) return 0f;
                float? remaining = _reloadTimer.RemainingTime(Runner);
                if (!remaining.HasValue) return 1f;
                return 1f - (remaining.Value / ReloadDuration);
            }
        }

        public override void Spawned()
        {
            // 부모(Player)에서 CardEffect 컴포넌트 찾기
            _cardEffect = GetComponentInParent<CardEffect>();
            CurrentAmmo = _magazineSize;
        }

        public override void FixedUpdateNetwork()
        {
            // 재장전 완료 체크
            if (IsReloading && _reloadTimer.Expired(Runner))
            {
                IsReloading = false;
                CurrentAmmo = _magazineSize;

                // 과충전 활성화 (배율이 1 초과일 때만)
                if (_cardEffect != null && _cardEffect.OverchargeDamageMultiplier > 1f)
                {
                    IsOvercharged = true;
                    Debug.Log($"[Colosseum] Gun: 과충전 활성화! 다음 발 x{_cardEffect.OverchargeDamageMultiplier}");
                }

                Debug.Log($"[Colosseum] Gun: 재장전 완료! 탄약: {CurrentAmmo}/{_magazineSize}");
            }

            if (GetInput(out NetworkInputData input))
            {
                // 마우스 월드 좌표 → 총 위치 기준 방향 계산
                Vector2 aimDir = (Vector2)input.AimDirection - (Vector2)transform.position;
                if (aimDir == Vector2.zero) aimDir = Vector2.right;
                _networkedAimDir = aimDir.normalized;

                // 재장전 중에는 발사 불가
                if (IsReloading) return;

                if (input.IsFirePressed && CurrentAmmo > 0 && _fireCooldown.ExpiredOrNotRunning(Runner))
                {
                    Fire();
                    CurrentAmmo--;

                    float cooldown = _fireRate / FireRateMultiplier;
                    _fireCooldown = TickTimer.CreateFromSeconds(Runner, cooldown);

                    // 탄창이 비면 자동 재장전 시작
                    if (CurrentAmmo <= 0)
                    {
                        StartReload();
                    }
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

        /// <summary>
        /// 산탄총 스프레드: 각 탄이 ±_spreadAngle 범위 내에서 랜덤 각도로 퍼진다.
        /// </summary>
        [Header("Spread Settings")]
        [SerializeField] private float _spreadAngle = 25f;

        private void Fire()
        {
            if (!Object.HasStateAuthority) return;

            Vector2 fireDir = _networkedAimDir;

            // 기본 1발은 정확히 조준 방향으로
            SpawnBullet(fireDir);

            // 추가 탄환은 산탄총처럼 랜덤 스프레드
            for (int i = 0; i < ExtraBullets; i++)
            {
                float randomAngle = Random.Range(-_spreadAngle, _spreadAngle);
                Vector2 spreadDir = Rotate(fireDir, randomAngle);
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
                    // 기본 카드 강화 적용
                    float dmgMul = _cardEffect.DamageMultiplier;

                    // 과충전: 첫 발만 데미지 증폭
                    if (IsOvercharged)
                    {
                        dmgMul *= _cardEffect.OverchargeDamageMultiplier;
                        IsOvercharged = false;
                        Debug.Log($"[Colosseum] Overcharged shot! DMG x{_cardEffect.OverchargeDamageMultiplier}");
                    }

                    bullet.DamageMultiplier = dmgMul;
                    bullet.SpeedMultiplier = _cardEffect.BulletSpeedMultiplier;
                    bullet.SizeMultiplier = _cardEffect.BulletSizeMultiplier;
                    bullet.BounceCount = _cardEffect.BonusBounce;
                    bullet.PierceCount = _cardEffect.BonusPierce;
                    bullet.KnockbackForce = _cardEffect.KnockbackForce;
                    bullet.ExplosionRadius = _cardEffect.ExplosionRadius;
                    bullet.ExplosionDamageRatio = _cardEffect.ExplosionDamageRatio;

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

        private void StartReload()
        {
            if (IsReloading) return;

            IsReloading = true;

            // CardEffect의 ReloadSpeedMultiplier 적용 (높을수록 빠르게 장전)
            float reloadSpeed = _reloadTime;
            if (_cardEffect != null)
            {
                reloadSpeed /= _cardEffect.ReloadSpeedMultiplier;
            }

            ReloadDuration = reloadSpeed;
            _reloadTimer = TickTimer.CreateFromSeconds(Runner, reloadSpeed);
            Debug.Log($"[Colosseum] Gun: 재장전 시작! ({reloadSpeed:F2}초)");
        }

        /// <summary>
        /// 외부에서 강제 재장전 (리스폰 시 탄약 리필 등)
        /// </summary>
        public void ForceReload()
        {
            IsReloading = false;
            CurrentAmmo = _magazineSize;
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
