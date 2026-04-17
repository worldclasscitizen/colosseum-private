using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

namespace Colosseum.UI
{
    /// <summary>
    /// 메인 메뉴 화면.
    /// MainMenu 씬의 빈 GameObject에 붙이면 자동으로 Canvas + 버튼을 생성.
    /// </summary>
    public class MainMenuUI : MonoBehaviour
    {
        [Header("Scene Names")]
        [SerializeField] private string _gameSceneName = "SampleScene";

        [Header("Button Style")]
        [SerializeField] private float _buttonWidth = 400f;
        [SerializeField] private float _buttonHeight = 55f;
        [SerializeField] private float _buttonSpacing = 12f;
        [SerializeField] private Color _buttonColor = new Color(0.15f, 0.15f, 0.2f, 0.95f);
        [SerializeField] private Color _buttonHoverColor = new Color(0.25f, 0.25f, 0.35f, 1f);
        [SerializeField] private Color _textColor = new Color(0.9f, 0.88f, 0.82f);

        private TextMeshProUGUI _noticeText;
        private float _noticeTimer;

        private void Start()
        {
            CreateUI();
        }

        private void Update()
        {
            // 알림 텍스트 자동 숨김
            if (_noticeText != null && _noticeText.gameObject.activeSelf)
            {
                _noticeTimer -= Time.deltaTime;
                if (_noticeTimer <= 0f)
                {
                    _noticeText.gameObject.SetActive(false);
                }
            }
        }

        private void CreateUI()
        {
            // ── Canvas ──
            var canvasObj = new GameObject("MainMenuCanvas");
            var canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            var scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            canvasObj.AddComponent<GraphicRaycaster>();

            // ── 배경 ──
            var bgObj = new GameObject("Background");
            bgObj.transform.SetParent(canvasObj.transform, false);
            var bgRect = bgObj.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;
            var bgImage = bgObj.AddComponent<Image>();
            bgImage.color = new Color(0.08f, 0.06f, 0.1f, 1f);

            // ── 중앙 컨테이너 ──
            var container = new GameObject("MenuContainer");
            container.transform.SetParent(canvasObj.transform, false);
            var containerRect = container.AddComponent<RectTransform>();
            containerRect.anchorMin = new Vector2(0.5f, 0.5f);
            containerRect.anchorMax = new Vector2(0.5f, 0.5f);
            containerRect.pivot = new Vector2(0.5f, 0.5f);
            containerRect.anchoredPosition = Vector2.zero;

            // 컨테이너 크기 직접 지정
            float totalHeight = 100f + 30f + (_buttonHeight + _buttonSpacing) * 5;
            containerRect.sizeDelta = new Vector2(_buttonWidth, totalHeight);

            var vlg = container.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = _buttonSpacing;
            vlg.childAlignment = TextAnchor.MiddleCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            // ── 타이틀: COLOSSEUM ──
            var titleObj = CreateTitle(container.transform);

            // ── 간격 ──
            CreateSpacer(container.transform, 30f);

            // ── 버튼 5개 ──
            CreateMenuButton(container.transform, "\u25b6  Create Room", OnCreateRoom);
            CreateMenuButton(container.transform, "\u25cb  Find Room", OnFindRoom);
            CreateMenuButton(container.transform, "\u2692  Dev Mode", OnDevMode);
            CreateMenuButton(container.transform, "\u2699  Settings", OnSettings);
            CreateMenuButton(container.transform, "\u2716  Quit Game", OnQuitGame);

            // ── 알림 텍스트 (화면 하단) ──
            var noticeObj = new GameObject("NoticeText");
            noticeObj.transform.SetParent(canvasObj.transform, false);
            var noticeRect = noticeObj.AddComponent<RectTransform>();
            noticeRect.anchorMin = new Vector2(0.5f, 0.15f);
            noticeRect.anchorMax = new Vector2(0.5f, 0.15f);
            noticeRect.pivot = new Vector2(0.5f, 0.5f);
            noticeRect.sizeDelta = new Vector2(800f, 50f);

            _noticeText = noticeObj.AddComponent<TextMeshProUGUI>();
            _noticeText.fontSize = 24;
            _noticeText.color = new Color(1f, 0.3f, 0.3f);
            _noticeText.alignment = TextAlignmentOptions.Center;
            _noticeText.fontStyle = FontStyles.Bold;
            noticeObj.SetActive(false);
        }

        private GameObject CreateTitle(Transform parent)
        {
            var titleObj = new GameObject("Title");
            titleObj.transform.SetParent(parent, false);

            var le = titleObj.AddComponent<LayoutElement>();
            le.preferredWidth = _buttonWidth;
            le.preferredHeight = 100f;

            var titleText = titleObj.AddComponent<TextMeshProUGUI>();
            titleText.text = "COLOSSEUM";
            titleText.fontSize = 72;
            titleText.color = new Color(0.85f, 0.75f, 0.55f); // 금색 톤
            titleText.alignment = TextAlignmentOptions.Center;
            titleText.fontStyle = FontStyles.Bold;
            titleText.characterSpacing = 8f;
            titleText.enableWordWrapping = false;

            return titleObj;
        }

        private void CreateSpacer(Transform parent, float height)
        {
            var spacer = new GameObject("Spacer");
            spacer.transform.SetParent(parent, false);
            var le = spacer.AddComponent<LayoutElement>();
            le.preferredHeight = height;
            le.preferredWidth = _buttonWidth;
        }

        private void CreateMenuButton(Transform parent, string label, UnityEngine.Events.UnityAction onClick)
        {
            var btnObj = new GameObject($"Btn_{label}");
            btnObj.transform.SetParent(parent, false);

            var le = btnObj.AddComponent<LayoutElement>();
            le.preferredWidth = _buttonWidth;
            le.preferredHeight = _buttonHeight;

            // 버튼 배경
            var btnImage = btnObj.AddComponent<Image>();
            btnImage.color = _buttonColor;

            // 버튼 컴포넌트
            var btn = btnObj.AddComponent<Button>();
            btn.targetGraphic = btnImage;
            btn.onClick.AddListener(onClick);

            // 호버 색상
            var colors = btn.colors;
            colors.normalColor = _buttonColor;
            colors.highlightedColor = _buttonHoverColor;
            colors.pressedColor = new Color(0.35f, 0.35f, 0.45f, 1f);
            colors.selectedColor = _buttonHoverColor;
            btn.colors = colors;

            // 텍스트
            var textObj = new GameObject("Text");
            textObj.transform.SetParent(btnObj.transform, false);
            var textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(20f, 0f);
            textRect.offsetMax = new Vector2(-20f, 0f);

            var tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = 26;
            tmp.color = _textColor;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontStyle = FontStyles.Normal;
        }

        // ── 버튼 콜백 ──

        private void OnCreateRoom()
        {
            ShowNotice("온라인 멀티 기능은 추후에 구현 예정입니다.");
        }

        private void OnFindRoom()
        {
            ShowNotice("온라인 멀티 기능은 추후에 구현 예정입니다.");
        }

        private void OnDevMode()
        {
            SceneManager.LoadScene(_gameSceneName);
        }

        private void OnSettings()
        {
            ShowNotice("설정 기능은 추후에 구현 예정입니다.");
        }

        private void OnQuitGame()
        {
            Debug.Log("[Colosseum] Quit Game");
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void ShowNotice(string message)
        {
            if (_noticeText == null) return;
            _noticeText.text = message;
            _noticeText.gameObject.SetActive(true);
            _noticeTimer = 3f;
        }
    }
}
