using UnityEngine;
using Fusion;
using Colosseum.Network;
using Colosseum.Game;
using Colosseum.Card;

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
        private PlayerHealth _health;
        private RoomManager _roomManager;
        private CardEffect _cardEffect;

        [Networked] private NetworkBool _isGrounded { get; set; }

        public override void Spawned()
        {
            _rb = GetComponent<Rigidbody2D>();
            _spriteRenderer = GetComponent<SpriteRenderer>();
            _health = GetComponent<PlayerHealth>();
            _roomManager = FindObjectOfType<RoomManager>();
            _cardEffect = GetComponent<CardEffect>();

            if (Object.HasInputAuthority)
                _spriteRenderer.color = Color.green;
            else
                _spriteRenderer.color = Color.red;
        }

        public override void FixedUpdateNetwork()
        {
            // 죽었으면 조작 불가
            if (_health != null && _health.IsDead) return;

            if (GetInput(out NetworkInputData input))
            {
                // 카드 강화 적용된 이동 속도
                float speed = _moveSpeed;
                float jump = _jumpForce;
                if (_cardEffect != null)
                {
                    speed *= _cardEffect.MoveSpeedMultiplier;
                    jump *= _cardEffect.JumpForceMultiplier;
                }

                _rb.velocity = new Vector2(input.Direction.x * speed, _rb.velocity.y);

                _isGrounded = Physics2D.OverlapBox(
                    _groundCheck.position, _groundCheckSize, 0f, _groundLayer) != null;

                if (input.IsJumpPressed && _isGrounded)
                    _rb.velocity = new Vector2(_rb.velocity.x, jump);

                if (input.Direction.x != 0)
                    _spriteRenderer.flipX = input.Direction.x < 0;

                // 방 끝 도달 감지
                CheckRoomBoundary();
            }
        }

        private void CheckRoomBoundary()
        {
            if (_roomManager == null) return;
            if (!Object.HasStateAuthority) return;

            var room = _roomManager.GetCurrentRoom();
            if (room == null) return;

            bool isPlayer1 = Object.InputAuthority == _roomManager.Player1;

            // Player1은 오른쪽 끝 도달 시 다음 방으로
            if (isPlayer1 && transform.position.x >= room.rightBound)
            {
                Debug.Log($"[Colosseum] Player1 reached right bound! LastKiller:{_roomManager.LastKiller}");
                if (_roomManager.TryAdvanceRoom(Object.InputAuthority, 1))
                {
                    OnRoomAdvanced();
                }
            }
            // Player2는 왼쪽 끝 도달 시 다음 방으로
            else if (!isPlayer1 && transform.position.x <= room.leftBound)
            {
                Debug.Log($"[Colosseum] Player2 reached left bound! LastKiller:{_roomManager.LastKiller}");
                if (_roomManager.TryAdvanceRoom(Object.InputAuthority, -1))
                {
                    OnRoomAdvanced();
                }
            }
        }

        private void OnRoomAdvanced()
        {
            // 카메라 전환
            var cam = FindObjectOfType<CameraSystem.CameraController>();
            if (cam != null)
            {
                cam.SetWinnerTarget(transform);
                cam.OnRoomChanged();
            }

            Debug.Log("[Colosseum] Room advanced!");
        }
    }
}
