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
        private CardDeck _cardDeck;

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
            _cardDeck = FindObjectOfType<CardDeck>();
        }

        public override void FixedUpdateNetwork()
        {
            if (!Object.HasStateAuthority) return;

            // 리스폰 타이머
            if (IsDead && _respawnTimer.Expired(Runner) && !IsWaitingForCardSelection())
            {
                DoRespawn();
            }

            // 맵 밖 사망 체크
            if (!IsDead && _roomManager != null)
            {
                if (_roomManager.IsOutOfBounds(transform.position))
                {
                    // 맵 밖으로 떨어지면 상대방이 킬한 것으로 처리
                    PlayerRef opponent = FindOpponentRef();
                    Debug.Log($"[Colosseum] Player out of bounds! Credited to: {opponent}");
                    Die(opponent);
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
            bool isSuicide = killer == default(PlayerRef) || killer == Object.InputAuthority;
            PlayerRef creditedKiller = isSuicide ? default(PlayerRef) : killer;

            IsDead = true;
            CurrentHealth = 0f;

            // 시각적 비활성화 (본인 + 자식 포함)
            SetVisuals(false);

            // 사망한 플레이어는 원인과 무관하게 카드 드로우 대상이다.
            if (_cardDeck == null)
            {
                _cardDeck = FindObjectOfType<CardDeck>();
            }

            if (_cardDeck != null)
            {
                _cardDeck.StartDraw(Object.InputAuthority);
            }

            // RoomManager에 킬 등록
            if (_roomManager != null)
            {
                _roomManager.RegisterKill(creditedKiller);
            }

            // 킬러에게 HealOnKill 적용
            ApplyHealOnKill(creditedKiller);

            _respawnTimer = TickTimer.CreateFromSeconds(Runner, _respawnDelay);
            Debug.Log($"[Colosseum] Player died! Killer: {creditedKiller} (original: {killer}, suicide:{isSuicide})");
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

            // 총 재장전 리셋
            var gun = GetComponentInChildren<Gun>();
            if (gun != null)
            {
                gun.ForceReload();
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

        private bool IsWaitingForCardSelection()
        {
            if (_cardDeck == null)
            {
                _cardDeck = FindObjectOfType<CardDeck>();
            }

            if (_cardDeck == null || _cardDeck.Object == null || !_cardDeck.Object.IsValid)
            {
                return false;
            }

            return _cardDeck.IsDrawing && _cardDeck.DrawingPlayer == Object.InputAuthority;
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

        private PlayerRef FindOpponentRef()
        {
            var players = FindObjectsOfType<PlayerHealth>();
            foreach (var p in players)
            {
                var netObj = p.GetComponent<Fusion.NetworkObject>();
                if (netObj != null && netObj.InputAuthority != Object.InputAuthority)
                {
                    return netObj.InputAuthority;
                }
            }
            // 솔로 테스트: 상대가 없으면 본인을 killer로 (자해 처리)
            return Object.InputAuthority;
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
            var players = FindObjectsOfType<PlayerHealth>();
            foreach (var p in players)
            {
                var netObj = p.GetComponent<NetworkObject>();
                if (netObj == null || netObj.InputAuthority != attacker) continue;

                p.CurrentHealth = Mathf.Min(p.CurrentHealth + healAmount, _maxHealth);
                Debug.Log($"[Colosseum] Lifesteal: healed {attacker} for {healAmount:F1} HP");
                break;
            }
        }

        private void ApplyHealOnKill(PlayerRef killer)
        {
            if (killer == default(PlayerRef)) return;

            // 킬러를 찾아서 CardEffect의 HealOnKill만큼 회복
            var players = FindObjectsOfType<PlayerHealth>();
            foreach (var p in players)
            {
                var netObj = p.GetComponent<NetworkObject>();
                if (netObj == null || netObj.InputAuthority != killer) continue;

                var cardEffect = p.GetComponent<CardEffect>();
                if (cardEffect != null && cardEffect.HealOnKill > 0f)
                {
                    p.CurrentHealth = Mathf.Min(p.CurrentHealth + cardEffect.HealOnKill, _maxHealth);
                    Debug.Log($"[Colosseum] HealOnKill: healed {killer} for {cardEffect.HealOnKill} HP");
                }
                break;
            }
        }
    }
}
