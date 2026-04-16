using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Colosseum.Game;

namespace Colosseum.UI
{
    public class RoomIndicatorUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI _roomText;
        [SerializeField] private Image[] _roomDots;

        [Header("Colors")]
        [SerializeField] private Color _currentRoomColor = Color.white;
        [SerializeField] private Color _otherRoomColor = new Color(0.4f, 0.4f, 0.4f, 0.6f);
        [SerializeField] private Color _player1SideColor = new Color(0.2f, 0.8f, 0.2f); // 초록
        [SerializeField] private Color _player2SideColor = new Color(0.8f, 0.2f, 0.2f); // 빨강

        private RoomManager _roomManager;
        private int _lastRoomIndex = -1;

        private void Update()
        {
            if (_roomManager == null)
            {
                _roomManager = FindObjectOfType<RoomManager>();
                return;
            }

            if (_roomManager.Object == null || !_roomManager.Object.IsValid) return;

            int currentRoom = _roomManager.CurrentRoomIndex;
            int totalRooms = _roomManager.RoomCount;

            if (currentRoom != _lastRoomIndex)
            {
                Debug.Log($"[Colosseum] RoomIndicator - Room:{currentRoom}, Total:{totalRooms}, Center:{totalRooms / 2}");
                _lastRoomIndex = currentRoom;
                UpdateDisplay(currentRoom);
            }
        }

        private void UpdateDisplay(int currentRoom)
        {
            int totalRooms = _roomManager.RoomCount;
            int centerRoom = totalRooms / 2;

            // 텍스트 업데이트
            if (_roomText != null)
            {
                if (currentRoom == centerRoom)
                    _roomText.text = "CENTER";
                else if (currentRoom < centerRoom)
                    _roomText.text = $"P2 SIDE ({centerRoom - currentRoom})";
                else
                    _roomText.text = $"P1 SIDE ({currentRoom - centerRoom})";
            }

            // 방 점 업데이트
            if (_roomDots != null)
            {
                for (int i = 0; i < _roomDots.Length; i++)
                {
                    if (i >= totalRooms)
                    {
                        _roomDots[i].gameObject.SetActive(false);
                        continue;
                    }

                    _roomDots[i].gameObject.SetActive(true);

                    if (i == currentRoom)
                    {
                        _roomDots[i].color = _currentRoomColor;
                        _roomDots[i].transform.localScale = Vector3.one * 1.3f;
                    }
                    else
                    {
                        _roomDots[i].color = _otherRoomColor;
                        _roomDots[i].transform.localScale = Vector3.one;
                    }
                }
            }
        }
    }
}
