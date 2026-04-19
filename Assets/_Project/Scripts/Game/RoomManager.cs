using UnityEngine;
using Fusion;
using System.Collections.Generic;

namespace Colosseum.Game
{
    public class RoomManager : NetworkBehaviour
    {
        [Header("Room Setup")]
        [SerializeField] private List<RoomData> _rooms = new();
        [SerializeField] private float _roomGap = 0f;

        [Header("Respawn")]
        [SerializeField] private float _maxRespawnDistance = 8f;
        [SerializeField] private float _outOfBoundsPadding = 3f;

        // 네트워크 동기화
        [Networked] public int CurrentRoomIndex { get; set; }
        [Networked] public PlayerRef LastKiller { get; set; }
        [Networked] public PlayerRef Player1 { get; set; }
        [Networked] public PlayerRef Player2 { get; set; }

        // Player1은 왼쪽 → 오른쪽 진행, Player2는 오른쪽 → 왼쪽 진행
        // Player1의 승리 = 마지막 방(오른쪽 끝) 도달
        // Player2의 승리 = 첫 번째 방(왼쪽 끝) 도달

        public override void Spawned()
        {
            if (Object.HasStateAuthority)
            {
                CalculateRoomBounds();
                CurrentRoomIndex = _rooms.Count / 2; // 중앙 방에서 시작
            }
        }

        private void CalculateRoomBounds()
        {
            float currentX = 0f;

            // 중앙 방을 원점으로 놓기 위해 전체 길이 계산
            float totalWidth = 0f;
            for (int i = 0; i < _rooms.Count; i++)
            {
                totalWidth += _rooms[i].width;
                if (i < _rooms.Count - 1) totalWidth += _roomGap;
            }

            currentX = -totalWidth / 2f;

            for (int i = 0; i < _rooms.Count; i++)
            {
                _rooms[i].leftBound = currentX;
                _rooms[i].rightBound = currentX + _rooms[i].width;
                _rooms[i].centerX = currentX + _rooms[i].width / 2f;
                currentX += _rooms[i].width + _roomGap;
            }
        }

        public RoomData GetCurrentRoom()
        {
            if (_rooms.Count == 0) return null;
            return _rooms[Mathf.Clamp(CurrentRoomIndex, 0, _rooms.Count - 1)];
        }

        public RoomData GetRoom(int index)
        {
            if (index < 0 || index >= _rooms.Count) return null;
            return _rooms[index];
        }

        public int RoomCount => _rooms.Count;

        /// <summary>
        /// 플레이어가 방 끝에 도달했을 때 호출.
        /// 승자만 다음 방으로 넘어갈 수 있음.
        /// </summary>
        public bool TryAdvanceRoom(PlayerRef player, int direction)
        {
            if (!Object.HasStateAuthority)
            {
                Debug.Log("[Colosseum] TryAdvance BLOCKED: no state authority");
                return false;
            }

            bool hasLastKiller = LastKiller != default(PlayerRef);

            // 마지막 킬 기록이 있을 때만 해당 승자에게 전진 권한을 제한한다.
            if (hasLastKiller && player != LastKiller)
            {
                Debug.Log($"[Colosseum] TryAdvance BLOCKED: player {player} != LastKiller {LastKiller}");
                return false;
            }

            // Player1(왼쪽)은 +1 방향, Player2(오른쪽)은 -1 방향으로만 진행
            int targetRoom = CurrentRoomIndex + direction;
            if (targetRoom < 0 || targetRoom >= _rooms.Count) 
            {
                // 맵 끝 도달 = 승리!
                OnGameWin(player);
                return false;
            }

            CurrentRoomIndex = targetRoom;
            Debug.Log($"[Colosseum] Room advanced to {CurrentRoomIndex} by {player}");
            return true;
        }

        /// <summary>
        /// 킬 발생 시 호출
        /// </summary>
        public void RegisterKill(PlayerRef killer)
        {
            if (!Object.HasStateAuthority) return;

            // Player:None(무효)으로는 LastKiller를 덮어쓰지 않음
            if (killer == default(PlayerRef))
            {
                Debug.Log($"[Colosseum] RegisterKill skipped: killer is None (suicide/out-of-bounds)");
                return;
            }

            LastKiller = killer;
            Debug.Log($"[Colosseum] Last killer set to: {killer}");
        }

        /// <summary>
        /// 리스폰 위치 계산.
        /// 상대방 위치 기준으로 본인 진영에 가까운 쪽에 리스폰.
        /// </summary>
        public Vector2 GetRespawnPosition(PlayerRef deadPlayer, Vector2 opponentPosition)
        {
            var room = GetCurrentRoom();
            if (room == null) return Vector2.zero;

            bool isPlayer1 = deadPlayer == Player1;
            // Player1은 왼쪽 진영, Player2는 오른쪽 진영
            float respawnX;

            if (isPlayer1)
            {
                // 상대보다 왼쪽(본인 진영)에 리스폰
                respawnX = opponentPosition.x - _maxRespawnDistance;
                // 방 왼쪽 경계 제한
                respawnX = Mathf.Max(respawnX, room.leftBound + 1f);
            }
            else
            {
                // 상대보다 오른쪽(본인 진영)에 리스폰
                respawnX = opponentPosition.x + _maxRespawnDistance;
                // 방 오른쪽 경계 제한
                respawnX = Mathf.Min(respawnX, room.rightBound - 1f);
            }

            return new Vector2(respawnX, room.cameraY + 2f);
        }

        /// <summary>
        /// 카메라 밖 + padding 초과 시 사망 판정
        /// </summary>
        public bool IsOutOfBounds(Vector2 playerPos)
        {
            var room = GetCurrentRoom();
            if (room == null) return false;

            float left = room.leftBound - _outOfBoundsPadding;
            float right = room.rightBound + _outOfBoundsPadding;

            return playerPos.x < left || playerPos.x > right;
        }

        private void OnGameWin(PlayerRef winner)
        {
            Debug.Log($"[Colosseum] GAME OVER! Winner: {winner}");

            string winnerName = winner == Player1 ? "Player 1" : "Player 2";
            var gameOverUI = FindObjectOfType<UI.GameOverUI>();
            if (gameOverUI != null)
            {
                gameOverUI.ShowGameOver(winnerName);
            }
        }
    }
}
