using UnityEngine;
using Fusion;
using Colosseum.Weapon;

namespace Colosseum.Player
{
    public class PlayerHealth : NetworkBehaviour
    {
        [Header("Health Settings")]
        [SerializeField] private float _maxHealth = 100f;

        [Networked, OnChangedRender(nameof(OnHealthChanged))]
        public float CurrentHealth { get; set; }

        [Networked] public NetworkBool IsDead { get; set; }

        // 카드 modifier
        [Networked] public float LifestealPercent { get; set; } = 0f;

        public override void Spawned()
        {
            if (Object.HasStateAuthority)
            {
                CurrentHealth = _maxHealth;
                IsDead = false;
            }
        }

        public void TakeDamage(float damage, PlayerRef attacker, BulletController.StatusEffect status)
        {
            if (!Object.HasStateAuthority) return;
            if (IsDead) return;

            CurrentHealth -= damage;
            Debug.Log($"[Colosseum] Player hit! HP: {CurrentHealth}/{_maxHealth}");

            // 상태이상 처리 (나중에 확장)
            if (status != BulletController.StatusEffect.None)
            {
                Debug.Log($"[Colosseum] Status effect applied: {status}");
            }

            // 흡혈 처리: 공격자 회복
            if (LifestealPercent > 0f)
            {
                // 나중에 카드 시스템에서 공격자 찾아서 회복
            }

            if (CurrentHealth <= 0f)
            {
                CurrentHealth = 0f;
                Die(attacker);
            }
        }

        private void Die(PlayerRef killer)
        {
            IsDead = true;
            Debug.Log($"[Colosseum] Player died! Killed by {killer}");
            // 나중에: 사망 → 카드 드로우 → 리스폰 → 카메라 전진
        }

        public void Respawn()
        {
            if (!Object.HasStateAuthority) return;
            CurrentHealth = _maxHealth;
            IsDead = false;
            Debug.Log($"[Colosseum] Player respawned!");
        }

        private void OnHealthChanged()
        {
            Debug.Log($"[Colosseum] Health changed: {CurrentHealth}/{_maxHealth}");
            // 나중에: HP UI 업데이트
        }
    }
}
