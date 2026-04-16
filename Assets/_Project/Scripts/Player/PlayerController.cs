using UnityEngine;
using Fusion;
using Colosseum.Network;

namespace Colosseum.Player
{
    public class PlayerController : NetworkBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float _moveSpeed = 8f;
        [SerializeField] private float _jumpForce = 12f;

        [Header("Ground Check")]
        [SerializeField] private Transform _groundCheck;
        [SerializeField] private Vector2 _groundCheckSize = new Vector2(0.8f, 0.1f);
        [SerializeField] private LayerMask _groundLayer;

        private Rigidbody2D _rb;
        private SpriteRenderer _spriteRenderer;

        [Networked] private NetworkBool _isGrounded { get; set; }

        public override void Spawned()
        {
            _rb = GetComponent<Rigidbody2D>();
            _spriteRenderer = GetComponent<SpriteRenderer>();

            if (Object.HasInputAuthority)
                _spriteRenderer.color = Color.green;
            else
                _spriteRenderer.color = Color.red;
        }

        public override void FixedUpdateNetwork()
        {
            if (GetInput(out NetworkInputData input))
            {
                _rb.velocity = new Vector2(input.Direction.x * _moveSpeed, _rb.velocity.y);

                _isGrounded = Physics2D.OverlapBox(
                    _groundCheck.position, _groundCheckSize, 0f, _groundLayer) != null;

                if (input.IsJumpPressed && _isGrounded)
                    _rb.velocity = new Vector2(_rb.velocity.x, _jumpForce);

                if (input.Direction.x != 0)
                    _spriteRenderer.flipX = input.Direction.x < 0;
            }
        }
    }
}
