using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Fusion;
using Colosseum.Card;

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

        private void Start()
        {
            _cardPanel.SetActive(false);

            for (int i = 0; i < _cardButtons.Length; i++)
            {
                int index = i;
                _cardButtons[i].onClick.AddListener(() => OnCardSelected(index));
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

            if (_cardDeck.IsDrawing && !_isShowing)
            {
                ShowCards();
            }
            else if (!_cardDeck.IsDrawing && _isShowing)
            {
                HideCards();
            }
        }

        private void ShowCards()
        {
            _isShowing = true;
            _cardPanel.SetActive(true);

            for (int i = 0; i < 3; i++)
            {
                CardData card = _cardDeck.GetDrawnCard(i);
                if (card != null)
                {
                    _cardNames[i].text = card.cardName;
                    _cardDescriptions[i].text = card.description;

                    if (_cardRarityBorders[i] != null)
                    {
                        _cardRarityBorders[i].color = GetRarityColor(card.rarity);
                    }
                }
            }

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void HideCards()
        {
            _isShowing = false;
            _cardPanel.SetActive(false);
        }

        private void OnCardSelected(int index)
        {
            if (_cardDeck == null) return;
            _cardDeck.SelectCard(index);
            Debug.Log($"[Colosseum] UI: Card {index} selected");
        }

        private Color GetRarityColor(CardRarity rarity)
        {
            switch (rarity)
            {
                case CardRarity.Common: return new Color(0.7f, 0.7f, 0.7f);
                case CardRarity.Rare: return new Color(0.2f, 0.5f, 1f);
                case CardRarity.Legendary: return new Color(1f, 0.8f, 0f);
                default: return Color.white;
            }
        }
    }
}
