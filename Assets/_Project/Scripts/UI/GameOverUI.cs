using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using Colosseum.Game;

namespace Colosseum.UI
{
    public class GameOverUI : MonoBehaviour
    {
        [Header("UI References (Optional - auto-creates if null)")]
        [SerializeField] private GameObject _gameOverPanel;
        [SerializeField] private TextMeshProUGUI _winnerText;

        private bool _isShowing = false;

        private void Start()
        {
            if (_gameOverPanel != null)
                _gameOverPanel.SetActive(false);
        }

        /// <summary>
        /// RoomManager에서 승리 시 호출
        /// </summary>
        public void ShowGameOver(string winnerName)
        {
            if (_isShowing) return;
            _isShowing = true;

            // Inspector 참조가 없거나 파괴된 경우 코드로 생성
            if (_gameOverPanel == null)
            {
                CreateGameOverUI();
            }

            _gameOverPanel.SetActive(true);
            _winnerText.text = $"{winnerName} Wins!";
            Debug.Log($"[Colosseum] Game Over UI shown: {winnerName} Wins!");
        }

        private void CreateGameOverUI()
        {
            // ── Canvas ──
            var canvasObj = new GameObject("GameOverCanvas");
            var canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 200;

            var scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            canvasObj.AddComponent<GraphicRaycaster>();

            // ── 패널 (반투명 배경) ──
            _gameOverPanel = new GameObject("GameOverPanel");
            _gameOverPanel.transform.SetParent(canvasObj.transform, false);

            var panelRect = _gameOverPanel.AddComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            var panelImage = _gameOverPanel.AddComponent<Image>();
            panelImage.color = new Color(0f, 0f, 0f, 0.7f);

            // ── 승자 텍스트 ──
            var textObj = new GameObject("WinnerText");
            textObj.transform.SetParent(_gameOverPanel.transform, false);

            var textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0.5f, 0.6f);
            textRect.anchorMax = new Vector2(0.5f, 0.6f);
            textRect.pivot = new Vector2(0.5f, 0.5f);
            textRect.sizeDelta = new Vector2(800f, 120f);

            _winnerText = textObj.AddComponent<TextMeshProUGUI>();
            _winnerText.fontSize = 72;
            _winnerText.color = new Color(0.85f, 0.75f, 0.55f);
            _winnerText.alignment = TextAlignmentOptions.Center;
            _winnerText.fontStyle = FontStyles.Bold;

            // ── "GAME OVER" 서브 텍스트 ──
            var subObj = new GameObject("GameOverLabel");
            subObj.transform.SetParent(_gameOverPanel.transform, false);

            var subRect = subObj.AddComponent<RectTransform>();
            subRect.anchorMin = new Vector2(0.5f, 0.45f);
            subRect.anchorMax = new Vector2(0.5f, 0.45f);
            subRect.pivot = new Vector2(0.5f, 0.5f);
            subRect.sizeDelta = new Vector2(600f, 60f);

            var subText = subObj.AddComponent<TextMeshProUGUI>();
            subText.text = "GAME OVER";
            subText.fontSize = 36;
            subText.color = new Color(0.7f, 0.7f, 0.7f);
            subText.alignment = TextAlignmentOptions.Center;

            // ── Main Menu 버튼 ──
            var btnObj = new GameObject("MainMenuButton");
            btnObj.transform.SetParent(_gameOverPanel.transform, false);

            var btnRect = btnObj.AddComponent<RectTransform>();
            btnRect.anchorMin = new Vector2(0.5f, 0.3f);
            btnRect.anchorMax = new Vector2(0.5f, 0.3f);
            btnRect.pivot = new Vector2(0.5f, 0.5f);
            btnRect.sizeDelta = new Vector2(300f, 55f);

            var btnImage = btnObj.AddComponent<Image>();
            btnImage.color = new Color(0.15f, 0.15f, 0.2f, 0.95f);

            var btn = btnObj.AddComponent<Button>();
            btn.targetGraphic = btnImage;
            btn.onClick.AddListener(OnMainMenuClicked);

            var colors = btn.colors;
            colors.highlightedColor = new Color(0.25f, 0.25f, 0.35f, 1f);
            colors.pressedColor = new Color(0.35f, 0.35f, 0.45f, 1f);
            btn.colors = colors;

            var btnTextObj = new GameObject("Text");
            btnTextObj.transform.SetParent(btnObj.transform, false);

            var btnTextRect = btnTextObj.AddComponent<RectTransform>();
            btnTextRect.anchorMin = Vector2.zero;
            btnTextRect.anchorMax = Vector2.one;
            btnTextRect.offsetMin = Vector2.zero;
            btnTextRect.offsetMax = Vector2.zero;

            var btnText = btnTextObj.AddComponent<TextMeshProUGUI>();
            btnText.text = "Main Menu";
            btnText.fontSize = 26;
            btnText.color = new Color(0.9f, 0.88f, 0.82f);
            btnText.alignment = TextAlignmentOptions.Center;
        }

        private void OnMainMenuClicked()
        {
            SceneManager.LoadScene("MainMenu");
        }
    }
}
