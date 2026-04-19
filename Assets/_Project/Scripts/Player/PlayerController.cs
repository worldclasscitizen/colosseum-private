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
        [SerializeField] private float _boundaryPadding = 0.25f;

        [Header("Ground Check")]
        [SerializeField] private Transform _groundCheck;
        [SerializeField] private Vector2 _groundCheckSize = new Vector2(0.8f, 0.1f);
        [SerializeField] private LayerMask _groundLayer;

        private Rigidbody2D _rb;
        private Collider2D _bodyCollider;
        private SpriteRenderer _spriteRenderer;
        private PlayerHealth _health;
        private RoomManager _roomManager;
        private CardEffect _cardEffect;

        [Networked] private NetworkBool _isGrounded { get; set; }

        public override void Spawned()
        {
            _rb = GetComponent<Rigidbody2D>();
            _bodyCollider = GetComponent<Collider2D>();
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
                CheckRoomBoundary(input.Direction.x);
            }
        }

        private void CheckRoomBoundary(float moveX)
        {
            if (_roomManager == null) return;
            if (!Object.HasStateAuthority) return;

            var room = _roomManager.GetCurrentRoom();
            if (room == null) return;

            bool isPlayer1 = Object.InputAuthority == _roomManager.Player1;
            float currentX = transform.position.x;
            float horizontalExtent = GetHorizontalBoundaryExtent();

            if (isPlayer1)
            {
                // 본인 진영(왼쪽) 쪽으로는 카메라 밖으로 나가지 못하게 막는다.
                float ownSideClampX = GetVisibleLeftBoundary(room) + horizontalExtent;
                bool pushingOwnSideOutward = moveX < 0f;
                if (currentX < ownSideClampX || (currentX <= ownSideClampX && pushingOwnSideOutward))
                {
                    ClampToX(ownSideClampX);
                    return;
                }

                // Player1은 오른쪽 끝 도달 시 다음 방으로
                float forwardBoundaryX = room.rightBound - horizontalExtent;
                bool pushingForward = moveX > 0f;
                if (currentX > forwardBoundaryX || (currentX >= forwardBoundaryX && pushingForward))
                {
                    Debug.Log($"[Colosseum] Player1 reached right bound! LastKiller:{_roomManager.LastKiller}");
                    if (_roomManager.TryAdvanceRoom(Object.InputAuthority, 1))
                    {
                        MoveIntoAdvancedRoom(isPlayer1);
                        OnRoomAdvanced();
                    }
                    else
                    {
                        ClampToX(forwardBoundaryX);
                    }
                }

                return;
            }

            // 본인 진영(오른쪽) 쪽으로는 카메라 밖으로 나가지 못하게 막는다.
            float ownSideRightClampX = GetVisibleRightBoundary(room) - horizontalExtent;
            bool pushingOwnSideOutwardRight = moveX > 0f;
            if (currentX > ownSideRightClampX || (currentX >= ownSideRightClampX && pushingOwnSideOutwardRight))
            {
                ClampToX(ownSideRightClampX);
                return;
            }

            // Player2는 왼쪽 끝 도달 시 다음 방으로
            float forwardLeftBoundaryX = room.leftBound + horizontalExtent;
            bool pushingForwardLeft = moveX < 0f;
            if (currentX < forwardLeftBoundaryX || (currentX <= forwardLeftBoundaryX && pushingForwardLeft))
            {
                Debug.Log($"[Colosseum] Player2 reached left bound! LastKiller:{_roomManager.LastKiller}");
                if (_roomManager.TryAdvanceRoom(Object.InputAuthority, -1))
                {
                    MoveIntoAdvancedRoom(isPlayer1);
                    OnRoomAdvanced();
                }
                else
                {
                    ClampToX(forwardLeftBoundaryX);
                }
            }
        }

        private void MoveIntoAdvancedRoom(bool isPlayer1)
        {
            var advancedRoom = _roomManager.GetCurrentRoom();
            if (advancedRoom == null) return;

            float horizontalExtent = GetHorizontalBoundaryExtent();
            float targetX = isPlayer1
                ? advancedRoom.leftBound + horizontalExtent
                : advancedRoom.rightBound - horizontalExtent;

            ClampToX(targetX);
        }

        private float GetHorizontalBoundaryExtent()
        {
            float extent = _boundaryPadding;

            if (_bodyCollider != null)
            {
                extent = Mathf.Max(extent, _bodyCollider.bounds.extents.x);
            }

            if (_spriteRenderer != null)
            {
                extent = Mathf.Max(extent, _spriteRenderer.bounds.extents.x);
            }

            return extent;
        }

        private float GetVisibleLeftBoundary(RoomData room)
        {
            var cam = UnityEngine.Camera.main;
            if (cam == null) return room.leftBound;

            return cam.transform.position.x - cam.orthographicSize * cam.aspect;
        }

        private float GetVisibleRightBoundary(RoomData room)
        {
            var cam = UnityEngine.Camera.main;
            if (cam == null) return room.rightBound;

            return cam.transform.position.x + cam.orthographicSize * cam.aspect;
        }

        private void ClampToX(float targetX)
        {
            Vector3 pos = transform.position;
            pos.x = targetX;
            transform.position = pos;

            if (_rb != null)
            {
                _rb.velocity = new Vector2(0f, _rb.velocity.y);
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
