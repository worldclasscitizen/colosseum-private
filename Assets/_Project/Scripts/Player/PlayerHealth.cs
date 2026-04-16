using UnityEngine;
using Fusion;
using Colosseum.Weapon;
using Colosseum.Game;
using Colosseum.Card;

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
        private SpriteRenderer _spriteRenderer;
        private Rigidbody2D _rb;

        public override void Spawned()
        {
            if (Object.HasStateAuthority)
            {
                CurrentHealth = _maxHealth;
                IsDead = false;
            }

            _roomManager = FindObjectOfType<RoomManager>();
            _spriteRenderer = GetComponent<SpriteRenderer>();
            _rb = GetComponent<Rigidbody2D>();
        }

        public override void FixedUpdateNetwork()
        {
            if (!Object.HasStateAuthority) return;

            // 리스폰 타이머
            if (IsDead && _respawnTimer.Expired(Runner))
            {
                DoRespawn();
            }

            // 맵 밖 사망 체크
            if (!IsDead && _roomManager != null)
            {
                if (_roomManager.IsOutOfBounds(transform.position))
                {
                    Debug.Log("[Colosseum] Player out of bounds!");
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

            // 라이프스틸 처리
            if (LifestealPercent > 0f && attacker != Object.InputAuthority)
            {
                HealAttacker(attacker, damage * LifestealPercent);
            }

            if (status != BulletController.StatusEffect.None)
            {
                Debug.Log($"[Colosseum] Status effect: {status}");
            }

            if (CurrentHealth <= 0f)
            {
                Die(attacker);
            }
        }

        private void Die(PlayerRef killer)
        {
            IsDead = true;
            CurrentHealth = 0f;

            // 시각적 비활성화 (본인 + 자식 포함)
            SetVisuals(false);

            // RoomManager에 킬 등록
            if (_roomManager != null)
            {
                _roomManager.RegisterKill(killer);
            }

            // 사망한 플레이어가 카드 드로우
            var cardDeck = FindObjectOfType<CardDeck>();
            if (cardDeck != null)
            {
                cardDeck.StartDraw(Object.InputAuthority);
            }

            _respawnTimer = TickTimer.CreateFromSeconds(Runner, _respawnDelay);
            Debug.Log($"[Colosseum] Player died! Killer: {killer}");
        }

        private void DoRespawn()
        {
            IsDead = false;
            CurrentHealth = _maxHealth;

            // 시각적 재활성화 (본인 + 자식 포함)
            SetVisuals(true);
            if (_rb != null)
            {
                _rb.simulated = true;
                _rb.velocity = Vector2.zero;
            }

            // 리스폰 위치 계산
            if (_roomManager != null)
            {
                Vector2 opponentPos = FindOpponentPosition();
                Vector2 respawnPos = _roomManager.GetRespawnPosition(Object.InputAuthority, opponentPos);
                transform.position = new Vector3(respawnPos.x, respawnPos.y, 0f);
            }

            Debug.Log("[Colosseum] Player respawned!");
        }

        public void ForceDeath(PlayerRef killer)
        {
            if (!Object.HasStateAuthority) return;
            if (IsDead) return;
            Die(killer);
        }

        private void OnHealthChanged()
        {
            // HP UI 업데이트용 (나중에 구현)
        }

        private void SetVisuals(bool visible)
        {
            // 본인 + 자식(Gun 등)의 모든 SpriteRenderer 제어
            var renderers = GetComponentsInChildren<SpriteRenderer>(true);
            foreach (var r in renderers)
            {
                r.enabled = visible;
            }
        }

        private Vector2 FindOpponentPosition()
        {
            var players = FindObjectsOfType<NetworkObject>();
            foreach (var p in players)
            {
                if (p.InputAuthority != Object.InputAuthority && p.GetComponent<PlayerHealth>() != null)
                {
                    return p.transform.position;
                }
            }
            return Vector2.zero;
        }

        private void HealAttacker(PlayerRef attacker, float healAmount)
        {
            var players = FindObjectsOfType<NetworkObject>();
            foreach (var p in players)
            {
                if (p.InputAuthority != attacker) continue;
                var health = p.GetComponent<PlayerHealth>();
                if (health != null)
                {
                    health.CurrentHealth = Mathf.Min(
                        health.CurrentHealth + healAmount, _maxHealth);
                }
                break;
            }
        }
    }
}
