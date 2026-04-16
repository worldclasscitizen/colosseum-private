using UnityEngine;

namespace Colosseum.Game
{
    [System.Serializable]
    public class RoomData
    {
        public string roomName = "Room";
        public float width = 16f;           // 방의 가로 길이
        public float cameraY = 0f;          // 카메라 Y 고정 위치
        public float cameraSize = 5f;       // 카메라 Orthographic Size
        public bool isWideRoom = false;     // true면 카메라가 플레이어 따라감

        // 방의 월드 좌표 기준 왼쪽/오른쪽 경계 (RoomManager가 계산)
        [HideInInspector] public float leftBound;
        [HideInInspector] public float rightBound;
        [HideInInspector] public float centerX;
    }
}
