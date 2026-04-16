using Colosseum.Player;
using UnityEngine;
using Fusion;

namespace Colosseum.Weapon
{
    public class BulletController : NetworkBehaviour
    {
        [Header("Base Stats")]
        [SerializeField] private float _baseDamage = 10f;
        [SerializeField] private float _baseSpeed = 15f;
        [SerializeField] private float _gravityScale = 1f;
        [SerializeField] private float _linearDrag = 0.5f;
        [SerializeField] private float _lifetime = 5f;

        [Networked] public float DamageMultiplier { get; set; } = 1f;
        [Networked] public float SpeedMultiplier { get; set; } = 1f;
        [Networked] public float SizeMultiplier { get; set; } = 1f;
        [Networked] public int BounceCount { get; set; } = 0;

        // 나중에 카드 시스템에서 사용할 상태이상
        public enum StatusEffect { None, Freeze, Burn, Poison }
        [Networked] public StatusEffect AppliedStatus { get; set; } = StatusEffect.None;

        [Networked] private TickTimer _lifetimeTimer { get; set; }
        [Networked] private int _remainingBounces { get; set; }
        [Networked] private PlayerRef _owner { get; set; }

        private Rigidbody2D _rb;

        public void Init(PlayerRef owner, Vector2 direction)
        {
            _owner = owner;
            _remainingBounces = BounceCount;

            _rb = GetComponent<Rigidbody2D>();
            _rb.gravityScale = _gravityScale;
            _rb.drag = _linearDrag;

            transform.localScale = Vector3.one * SizeMultiplier;

            float speed = _baseSpeed * SpeedMultiplier;
            _rb.velocity = direction.normalized * speed;

            _lifetimeTimer = TickTimer.CreateFromSeconds(Runner, _lifetime);
        }

        public override void FixedUpdateNetwork()
        {
            // 수명 초과 시 소멸
            if (_lifetimeTimer.Expired(Runner))
            {
                Runner.Despawn(Object);
                return;
            }
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (!Object.HasStateAuthority) return;

            // 플레이어에게 맞았을 때 (자기 자신 포함)
            var health = collision.gameObject.GetComponent<PlayerHealth>();
            if (health != null)
            {
                float finalDamage = _baseDamage * DamageMultiplier;
                health.TakeDamage(finalDamage, _owner, AppliedStatus);
                Runner.Despawn(Object);
                return;
            }

            // 벽/바닥에 맞았을 때
            if (_remainingBounces > 0)
            {
                _remainingBounces--;
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
