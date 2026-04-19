using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Fusion;
using Colosseum.Card;
using System.Linq;

namespace Colosseum.UI
{
    public class CardSelectionUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject _cardPanel;
        [SerializeField] private Button[] _cardButtons = new Button[3];
        [SerializeField] private TextMeshProUGUI[] _cardNames = new TextMeshProUGUI[3];
        [SerializeField] private TextMeshProUGUI[] _cardDescriptions = new TextMeshProUGUI[3];
        [SerializeField] private Image[] _cardRarityBorders = new Image[3];

        private CardDeck _cardDeck;
        private bool _isShowing = false;
        private bool _usingRuntimePanel = false;

        private void Start()
        {
            AutoBindReferences();

            var rootRect = transform as RectTransform;
            if (rootRect != null)
            {
                if (rootRect.localScale == Vector3.zero)
                {
                    rootRect.localScale = Vector3.one;
                }

                rootRect.anchorMin = Vector2.zero;
                rootRect.anchorMax = Vector2.one;
                rootRect.pivot = new Vector2(0.5f, 0.5f);
                rootRect.anchoredPosition = Vector2.zero;
                rootRect.sizeDelta = Vector2.zero;
            }

            var canvas = GetComponent<Canvas>();
            if (canvas != null)
            {
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = Mathf.Max(canvas.sortingOrder, 150);
            }

            if (_cardPanel != null)
            {
                var panelRect = _cardPanel.transform as RectTransform;
                if (panelRect != null)
                {
                    panelRect.anchorMin = new Vector2(0.5f, 0.5f);
                    panelRect.anchorMax = new Vector2(0.5f, 0.5f);
                    panelRect.pivot = new Vector2(0.5f, 0.5f);
                    panelRect.anchoredPosition = Vector2.zero;
                }

                _cardPanel.SetActive(false);
            }

            for (int i = 0; i < _cardButtons.Length; i++)
            {
                if (_cardButtons[i] == null) continue;
                int index = i;
                _cardButtons[i].onClick.AddListener(() => OnCardSelected(index));
            }

        }

        private void AutoBindReferences()
        {
            if (_cardPanel == null)
            {
                Transform panelTransform = GetComponentsInChildren<Transform>(true)
                    .FirstOrDefault(t => t.name == "CardPanel");
                if (panelTransform != null)
                {
                    _cardPanel = panelTransform.gameObject;
                }
            }

            if (_cardButtons == null || _cardButtons.Length != 3)
                _cardButtons = new Button[3];
            if (_cardNames == null || _cardNames.Length != 3)
                _cardNames = new TextMeshProUGUI[3];
            if (_cardDescriptions == null || _cardDescriptions.Length != 3)
                _cardDescriptions = new TextMeshProUGUI[3];
            if (_cardRarityBorders == null || _cardRarityBorders.Length != 3)
                _cardRarityBorders = new Image[3];

            if (_cardPanel == null)
            {
                return;
            }

            for (int i = 0; i < 3; i++)
            {
                Transform cardTransform = _cardPanel.GetComponentsInChildren<Transform>(true)
                    .FirstOrDefault(t => t.name == $"Card{i}");
                if (cardTransform == null)
                {
                    continue;
                }

                if (_cardButtons[i] == null)
                {
                    _cardButtons[i] = cardTransform.GetComponent<Button>();
                }

                if (_cardRarityBorders[i] == null)
                {
                    _cardRarityBorders[i] = cardTransform.GetComponent<Image>();
                }

                if (_cardNames[i] == null)
                {
                    Transform nameTransform = cardTransform.Find("CardName");
                    if (nameTransform != null)
                    {
                        _cardNames[i] = nameTransform.GetComponent<TextMeshProUGUI>();
                    }
                }

                if (_cardDescriptions[i] == null)
                {
                    Transform descriptionTransform = cardTransform.Find("CardDesc");
                    if (descriptionTransform != null)
                    {
                        _cardDescriptions[i] = descriptionTransform.GetComponent<TextMeshProUGUI>();
                    }
                }
            }
        }

        private void Update()
        {
            if (_cardDeck == null)
            {
                _cardDeck = FindObjectOfType<CardDeck>();
                return;
            }

            // Spawned 전에는 Networked 속성 접근 불가
            if (_cardDeck.Object == null || !_cardDeck.Object.IsValid) return;

            // _cardPanel이 파괴되었으면 카드 UI 표시 불가
            if (_cardPanel == null)
            {
                AutoBindReferences();
                if (_cardPanel == null) return;
            }

            bool shouldShow = _cardDeck.IsDrawing;

            if (shouldShow && !_isShowing)
            {
                ShowCards();
            }
            else if (!shouldShow && _isShowing)
            {
                HideCards();
            }
        }

        public void ShowCards()
        {
            EnsureRuntimePanel();

            if (_cardDeck == null)
            {
                _cardDeck = FindObjectOfType<CardDeck>();
            }

            if (_cardPanel == null)
            {
                return;
            }

            _isShowing = true;
            _cardPanel.SetActive(true);

            if (_cardDeck != null)
            {
                for (int i = 0; i < 3; i++)
                {
                    CardData card = _cardDeck.GetDrawnCard(i);
                    if (card != null)
                    {
                        if (_cardNames[i] != null)
                        {
                            _cardNames[i].text = card.cardName;
                        }

                        if (_cardDescriptions[i] != null)
                        {
                            _cardDescriptions[i].text = card.description;
                        }

                        if (_cardRarityBorders[i] != null)
                        {
                            _cardRarityBorders[i].color = GetRarityColor(card.rarity);
                        }
                    }
                }
            }

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        public void HideCards()
        {
            EnsureRuntimePanel();

            if (_cardPanel == null)
            {
                return;
            }

            _isShowing = false;
            _cardPanel.SetActive(false);
        }

        private void EnsureRuntimePanel()
        {
            if (_usingRuntimePanel && _cardPanel != null)
            {
                return;
            }

            if (_cardPanel != null)
            {
                _cardPanel.SetActive(false);
            }

            _cardButtons = new Button[3];
            _cardNames = new TextMeshProUGUI[3];
            _cardDescriptions = new TextMeshProUGUI[3];
            _cardRarityBorders = new Image[3];

            var panel = new GameObject("RuntimeCardPanel");
            panel.transform.SetParent(transform, false);
            panel.layer = gameObject.layer;

            var panelRect = panel.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.anchoredPosition = Vector2.zero;
            panelRect.sizeDelta = new Vector2(960f, 360f);

            var panelImage = panel.AddComponent<Image>();
            panelImage.color = new Color(0.05f, 0.06f, 0.08f, 0.92f);

            for (int i = 0; i < 3; i++)
            {
                float x = (i - 1) * 300f;

                var cardObject = new GameObject($"RuntimeCard{i}");
                cardObject.transform.SetParent(panel.transform, false);
                cardObject.layer = gameObject.layer;

                var cardRect = cardObject.AddComponent<RectTransform>();
                cardRect.anchorMin = new Vector2(0.5f, 0.5f);
                cardRect.anchorMax = new Vector2(0.5f, 0.5f);
                cardRect.pivot = new Vector2(0.5f, 0.5f);
                cardRect.anchoredPosition = new Vector2(x, 0f);
                cardRect.sizeDelta = new Vector2(250f, 300f);

                var cardImage = cardObject.AddComponent<Image>();
                cardImage.color = new Color(0.9f, 0.9f, 0.9f, 1f);

                var button = cardObject.AddComponent<Button>();
                int index = i;
                button.onClick.AddListener(() => OnCardSelected(index));

                _cardButtons[i] = button;
                _cardRarityBorders[i] = cardImage;

                var nameObject = new GameObject("CardName");
                nameObject.transform.SetParent(cardObject.transform, false);
                nameObject.layer = gameObject.layer;

                var nameRect = nameObject.AddComponent<RectTransform>();
                nameRect.anchorMin = new Vector2(0.1f, 0.65f);
                nameRect.anchorMax = new Vector2(0.9f, 0.9f);
                nameRect.offsetMin = Vector2.zero;
                nameRect.offsetMax = Vector2.zero;

                var nameText = nameObject.AddComponent<TextMeshProUGUI>();
                nameText.fontSize = 28;
                nameText.alignment = TextAlignmentOptions.Center;
                nameText.color = new Color(0.12f, 0.12f, 0.14f);
                nameText.enableWordWrapping = true;
                _cardNames[i] = nameText;

                var descObject = new GameObject("CardDesc");
                descObject.transform.SetParent(cardObject.transform, false);
                descObject.layer = gameObject.layer;

                var descRect = descObject.AddComponent<RectTransform>();
                descRect.anchorMin = new Vector2(0.1f, 0.15f);
                descRect.anchorMax = new Vector2(0.9f, 0.6f);
                descRect.offsetMin = Vector2.zero;
                descRect.offsetMax = Vector2.zero;

                var descText = descObject.AddComponent<TextMeshProUGUI>();
                descText.fontSize = 18;
                descText.alignment = TextAlignmentOptions.TopLeft;
                descText.color = new Color(0.2f, 0.2f, 0.24f);
                descText.enableWordWrapping = true;
                _cardDescriptions[i] = descText;
            }

            panel.SetActive(false);
            _cardPanel = panel;
            _usingRuntimePanel = true;
        }

        private void OnCardSelected(int index)
        {
            if (_cardDeck == null) return;
            if (!_cardDeck.IsDrawing) return;
            _cardDeck.SelectCard(index);
            Debug.Log($"[Colosseum] UI: Card {index} selected");
        }

        private Color GetRarityColor(CardRarity rarity)
        {
            switch (rarity)
            {
                case CardRarity.Common: return new Color(0.7f, 0.7f, 0.7f);
                case CardRarity.Rare: return new Color(0.2f, 0.5f, 1f);
                case CardRarity.Legendary: return new Color(1f, 0.84f, 0f);
                default: return Color.white;
            }
        }
    }
}
