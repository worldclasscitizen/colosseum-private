using UnityEngine;
using UnityEngine.UI;
using Colosseum.Game;

namespace Colosseum.UI
{
    /// <summary>
    /// 화면 상단 중앙에 9칸 박스를 표시.
    /// 현재 방 인덱스에 해당하는 박스만 색칠됨.
    /// 코드로 UI를 생성하므로 인스펙터 설정 불필요.
    /// </summary>
    public class RoomIndicatorUI : MonoBehaviour
    {
        [Header("Appearance")]
        [SerializeField] private float _boxSize = 22f;
        [SerializeField] private float _boxSpacing = 4f;
        [SerializeField] private float _borderWidth = 2f;
        [SerializeField] private float _topMargin = 20f;

        [Header("Colors")]
        [SerializeField] private Color _emptyColor = new Color(0.15f, 0.15f, 0.15f, 0.8f);
        [SerializeField] private Color _borderColor = new Color(0.6f, 0.6f, 0.6f, 0.9f);
        [SerializeField] private Color _currentRoomColor = Color.white;

        private RoomManager _roomManager;
        private Image[] _boxImages;
        private int _lastRoomIndex = -1;
        private bool _initialized = false;

        private void Start()
        {
            // 기존 자식(Text 등) 제거
            foreach (Transform child in transform)
            {
                Destroy(child.gameObject);
            }
        }

        private void Update()
        {
            if (_roomManager == null)
            {
                _roomManager = FindObjectOfType<RoomManager>();
                return;
            }

            if (_roomManager.Object == null || !_roomManager.Object.IsValid) return;

            int totalRooms = _roomManager.RoomCount;
            if (totalRooms <= 0) return;

            // 방 수가 확정되면 UI 생성
            if (!_initialized)
            {
                CreateBoxes(totalRooms);
                _initialized = true;
            }

            int currentRoom = _roomManager.CurrentRoomIndex;
            if (currentRoom != _lastRoomIndex)
            {
                _lastRoomIndex = currentRoom;
                UpdateBoxes(currentRoom);
            }
        }

        private void CreateBoxes(int count)
        {
            _boxImages = new Image[count];

            // 컨테이너: 화면 상단 중앙에 가로 정렬
            var container = new GameObject("BoxContainer");
            container.transform.SetParent(transform, false);

            var containerRect = container.AddComponent<RectTransform>();
            containerRect.anchorMin = new Vector2(0.5f, 1f);
            containerRect.anchorMax = new Vector2(0.5f, 1f);
            containerRect.pivot = new Vector2(0.5f, 1f);

            float totalWidth = count * (_boxSize + _borderWidth * 2) + (count - 1) * _boxSpacing;
            float totalHeight = _boxSize + _borderWidth * 2;
            containerRect.sizeDelta = new Vector2(totalWidth, totalHeight);
            containerRect.anchoredPosition = new Vector2(0f, -_topMargin);

            // HorizontalLayoutGroup
            var layout = container.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = _boxSpacing;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;

            for (int i = 0; i < count; i++)
            {
                // 보더 (바깥 박스)
                var borderObj = new GameObject($"Border_{i}");
                borderObj.transform.SetParent(container.transform, false);
                var borderImage = borderObj.AddComponent<Image>();
                borderImage.color = _borderColor;

                // 내부 박스 (보더 안쪽)
                var boxObj = new GameObject($"Box_{i}");
                boxObj.transform.SetParent(borderObj.transform, false);

                // Image를 먼저 추가해야 RectTransform이 자동 생성됨
                var boxImage = boxObj.AddComponent<Image>();

                var boxRect = boxObj.GetComponent<RectTransform>();
                boxRect.anchorMin = Vector2.zero;
                boxRect.anchorMax = Vector2.one;
                boxRect.offsetMin = new Vector2(_borderWidth, _borderWidth);
                boxRect.offsetMax = new Vector2(-_borderWidth, -_borderWidth);
                boxImage.color = _emptyColor;
                _boxImages[i] = boxImage;
            }
        }

        private void UpdateBoxes(int currentRoom)
        {
            if (_boxImages == null) return;

            for (int i = 0; i < _boxImages.Length; i++)
            {
                _boxImages[i].color = (i == currentRoom) ? _currentRoomColor : _emptyColor;
            }
        }
    }
}
