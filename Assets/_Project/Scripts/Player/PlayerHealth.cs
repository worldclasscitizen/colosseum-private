using UnityEngine;
using Fusion;
using Colosseum.Weapon;
using Colosseum.Game;

namespace Colosseum.Player
{
    public class PlayerHealth : NetworkBehaviour
    {
        [Header("Health Settings")]
        [SerializeField] private float _maxHealth = 100f;
        [SerializeField] private float _respawnDelay = 2f;

        [Networked, OnChangedRender(nameof(OnHealthChanged))]
        public float CurrentHealth { get; set; }

        [Networked] public NetworkBool IsDead { get; set; }
        [Networked] public float LifestealPercent { get; set; } = 0f;
        [Networked] private TickTimer _respawnTimer { get; set; }

        private RoomManager _roomManager;

        public override void Spawned()
        {
            if (Object.HasStateAuthority)
            {
                CurrentHealth = _maxHealth;
                IsDead = false;
            }

            _roomManager = FindObjectOfType<RoomManager>();
        }

        public override void FixedUpdateNetwork()
        {
            if (!Object.HasStateAuthority) return;

            // 리스폰 타이머
            if (IsDead && _respawnTimer.Expired(Runner))
            {
                DoRespawn();
            }

            // 카메라 밖 padding 초과 사망 체크
            if (!IsDead && _roomManager != null)
            {
                if (_roomManager.IsOutOfBounds(transform.position))
                {
                    Debug.Log($"[Colosseum] Player out of bounds!");
                    Die(_roomManager.LastKiller);
                }
            }
        }

        public void TakeDamage(float damage, PlayerRef attacker, BulletController.StatusEffect status)
        {
            if (!Object.HasStateAuthority) return;
            if (IsDead) return;

            CurrentHealth -= damage;
            Debug.Log($"[Colosseum] Player hit! HP: {CurrentHealth}/{_maxHealth}");

            if (status != BulletController.StatusEffect.None)
            {
                Debug.Log($"[Colosseum] Status effect: {status}");
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

            // RoomManager에 킬 등록
            if (_roomManager != null)
            {
                _roomManager.RegisterKill(killer);
            }

            // 캐릭터 비활성화 (시각적으로)
            var sr = GetComponent<SpriteRenderer>();
            if (sr != null) sr.enabled = false;

            var rb = GetComponent<Rigidbody2D>();
            if (rb != null) rb.simulated = false;

            // 리스폰 타이머 시작
            _respawnTimer = TickTimer.CreateFromSeconds(Runner, _respawnDelay);

            // 나중에: 카드 드로우 UI 표시
        }

        private void DoRespawn()
        {
            if (_roomManager == null) return;

            // 상대방 위치 찾기
            Vector2 opponentPos = FindOpponentPosition();

            // 리스폰 위치 계산
            Vector2 respawnPos = _roomManager.GetRespawnPosition(
                Object.InputAuthority, opponentPos);

            // 위치 이동 및 상태 복구
            transform.position = new Vector3(respawnPos.x, respawnPos.y, 0f);
            CurrentHealth = _maxHealth;
            IsDead = false;

            var sr = GetComponent<SpriteRenderer>();
            if (sr != null) sr.enabled = true;

            var rb = GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.simulated = true;
                rb.velocity = Vector2.zero;
            }

            Debug.Log($"[Colosseum] Player respawned at {respawnPos}");
        }

        private Vector2 FindOpponentPosition()
        {
            var players = FindObjectsOfType<PlayerHealth>();
            foreach (var p in players)
            {
                if (p != this && p.Object != null)
                {
                    return p.transform.position;
                }
            }
            return Vector2.zero;
        }

        /// <summary>
        /// 외부에서 강제 사망 (방 전환 시 사용)
        /// </summary>
        public void ForceDeath(PlayerRef killer)
        {
            if (!Object.HasStateAuthority) return;
            if (IsDead) return;
            Die(killer);
        }

        private void OnHealthChanged()
        {
            Debug.Log($"[Colosseum] Health: {CurrentHealth}/{_maxHealth}");
        }
    }
}
