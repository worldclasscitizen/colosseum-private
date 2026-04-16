using UnityEngine;
using Fusion;

namespace Colosseum.CameraSystem
{
    public class CameraController : NetworkBehaviour
    {
        [Header("References")]
        [SerializeField] private Game.RoomManager _roomManager;

        [Header("Follow Settings")]
        [SerializeField] private float _followSpeed = 5f;

        private UnityEngine.Camera _camera;

        // 네트워크 동기화: 현재 카메라 목표 위치
        [Networked] private Vector2 _targetPosition { get; set; }
        [Networked] private float _targetSize { get; set; }

        private Transform _winnerTransform;

        public override void Spawned()
        {
            _camera = UnityEngine.Camera.main;
            if (_camera == null)
            {
                Debug.LogError("[Colosseum] Main Camera not found!");
                return;
            }
        }

        public override void FixedUpdateNetwork()
        {
            if (!Object.HasStateAuthority) return;

            var room = _roomManager.GetCurrentRoom();
            if (room == null) return;

            if (room.isWideRoom && _winnerTransform != null)
            {
                // 넓은 방: 승자 기준으로 카메라 추적
                float clampedX = Mathf.Clamp(
                    _winnerTransform.position.x,
                    room.leftBound + _camera.orthographicSize * _camera.aspect,
                    room.rightBound - _camera.orthographicSize * _camera.aspect);

                _targetPosition = new Vector2(clampedX, room.cameraY);
            }
            else
            {
                // 좁은 방: 카메라 고정
                _targetPosition = new Vector2(room.centerX, room.cameraY);
            }

            _targetSize = room.cameraSize;
        }

        public override void Render()
        {
            if (_camera == null) return;

            var room = _roomManager.GetCurrentRoom();
            if (room == null) return;

            if (room.isWideRoom)
            {
                // 넓은 방에서는 부드럽게 따라감
                Vector3 pos = _camera.transform.position;
                pos.x = Mathf.Lerp(pos.x, _targetPosition.x, _followSpeed * Ti