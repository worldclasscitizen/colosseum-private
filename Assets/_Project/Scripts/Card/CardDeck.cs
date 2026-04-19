using UnityEngine;
using Fusion;
using System.Collections.Generic;
using Colosseum.UI;
using TMPro;
using System.Linq;
using System.Reflection;

namespace Colosseum.Card
{
    public class CardDeck : NetworkBehaviour
    {
        [Header("Card Pool")]
        [SerializeField] private List<CardData> _allCards = new();

        [Header("Draw Settings")]
        [SerializeField] private int _drawChoices = 3;

        // 드로우할 때 선택지를 담는 배열 (최대 3장)
        [Networked, Capacity(3)] private NetworkArray<int> _drawnCardIndices => default;
        [Networked] public NetworkBool IsDrawing { get; set; }
        [Networked] public PlayerRef DrawingPlayer { get; set; }

        /// <summary>
        /// 카드 드로우 시작 — 랜덤 3장 뽑아서 선택지 제공
        /// </summary>
        public void StartDraw(PlayerRef player)
        {
            if (!Object.HasStateAuthority) return;
            if (_allCards.Count == 0)
            {
                Debug.LogWarning("[Colosseum] No cards in deck!");
                return;
            }

            IsDrawing = true;
            DrawingPlayer = player;

            // 랜덤으로 카드 인덱스 뽑기 (중복 허용)
            for (int i = 0; i < _drawChoices; i++)
            {
                int randomIndex = Random.Range(0, _allCards.Count);
                _drawnCardIndices.Set(i, randomIndex);
            }

            Debug.Log($"[Colosseum] Draw started for {player}: " +
                $"{GetDrawnCard(0).cardName}, {GetDrawnCard(1).cardName}, {GetDrawnCard(2).cardName}");

            var cardSelectionUI = FindObjectOfType<CardSelectionUI>();
            if (cardSelectionUI != null)
            {
                cardSelectionUI.ShowCards();
                ForceCardPanelState(cardSelectionUI, true);
            }

            ShowFallbackCardPanel();
        }

        /// <summary>
        /// 카드 선택 (나중에 UI에서 호출)
        /// </summary>
        public void SelectCard(int choiceIndex)
        {
            if (!Object.HasStateAuthority) return;
            if (!IsDrawing) return;

            CardData selected = GetDrawnCard(choiceIndex);
            if (selected == null) return;

            ApplyCard(DrawingPlayer, selected);

            IsDrawing = false;
            var cardSelectionUI = FindObjectOfType<CardSelectionUI>();
            if (cardSelectionUI != null)
            {
                cardSelectionUI.HideCards();
                ForceCardPanelState(cardSelectionUI, false);
            }
            HideFallbackCardPanel();
            Debug.Log($"[Colosseum] {DrawingPlayer} selected: {selected.cardName}");
        }

        public CardData GetDrawnCard(int choiceIndex)
        {
            if (choiceIndex < 0 || choiceIndex >= _drawChoices) return null;
            int cardIndex = _drawnCardIndices[choiceIndex];
            if (cardIndex < 0 || cardIndex >= _allCards.Count) return null;
            return _allCards[cardIndex];
        }

        private void ApplyCard(PlayerRef player, CardData card)
        {
            // PlayerHealth로 플레이어 오브젝트를 특정 (총알 등 다른 NetworkObject 제외)
            var allHealth = FindObjectsOfType<Player.PlayerHealth>();
            foreach (var health in allHealth)
            {
                var netObj = health.GetComponent<Fusion.NetworkObject>();
                if (netObj == null || netObj.InputAuthority != player) continue;

                // 총 강화 → Gun에 직접 적용
                var gun = netObj.GetComponentInChildren<Weapon.Gun>();
                if (gun != null)
                {
                    gun.FireRateMultiplier *= card.fireRateMultiplier;
                    gun.ExtraBullets += card.extraBullets;
                }

                // 플레이어 HP 관련
                if (card.healAmount > 0)
                    health.CurrentHealth = Mathf.Min(
                        health.CurrentHealth + card.healAmount, 100f);
                health.LifestealPercent += card.lifestealPercent;

                // 총알/이동 강화 → CardEffect에 누적
                var cardEffect = netObj.GetComponent<CardEffect>();
                if (cardEffect != null)
                {
                    cardEffect.ApplyCard(card);
                    Debug.Log($"[Colosseum] CardEffect AFTER apply - SizeMul:{cardEffect.BulletSizeMultiplier}, SpeedMul:{cardEffect.BulletSpeedMultiplier}, Bounce:{cardEffect.BonusBounce}");
                }
                else
                {
                    Debug.LogWarning("[Colosseum] CardEffect NOT FOUND on player!");
                }

                Debug.Log($"[Colosseum] Card applied: {card.cardName} to {player}");
                break;
            }
        }

        private void ShowFallbackCardPanel()
        {
            GameObject panel = FindInactiveCardPanel();
            if (panel == null)
            {
                return;
            }

            ApplyDrawnCardTexts(panel);
            panel.SetActive(true);
        }

        private void ApplyDrawnCardTexts(GameObject panel)
        {
            for (int i = 0; i < _drawChoices; i++)
            {
                CardData card = GetDrawnCard(i);
                if (card == null)
                {
                    continue;
                }

                Transform cardTransform = panel.transform.Find($"Card{i}");
                if (cardTransform == null)
                {
                    continue;
                }

                Transform nameTransform = cardTransform.Find("CardName");
                if (nameTransform != null)
                {
                    var nameText = nameTransform.GetComponent<TextMeshProUGUI>();
                    if (nameText != null)
                    {
                        nameText.text = card.cardName;
                    }
                }

                Transform descriptionTransform = cardTransform.Find("CardDesc");
                if (descriptionTransform != null)
                {
                    var descriptionText = descriptionTransform.GetComponent<TextMeshProUGUI>();
                    if (descriptionText != null)
                    {
                        descriptionText.text = card.description;
                    }
                }
            }
        }

        private void HideFallbackCardPanel()
        {
            GameObject panel = FindInactiveCardPanel();
            if (panel != null)
            {
                panel.SetActive(false);
            }
        }

        private GameObject FindInactiveCardPanel()
        {
            var panelTransform = Resources.FindObjectsOfTypeAll<Transform>()
                .FirstOrDefault(t => t.name == "CardPanel");

            return panelTransform != null ? panelTransform.gameObject : null;
        }

        private void ForceCardPanelState(CardSelectionUI cardSelectionUI, bool active)
        {
            FieldInfo field = typeof(CardSelectionUI).GetField("_cardPanel", BindingFlags.Instance | BindingFlags.NonPublic);
            GameObject panel = field?.GetValue(cardSelectionUI) as GameObject;
            if (panel == null)
            {
                return;
            }

            if (active)
            {
                ApplyDrawnCardTexts(panel);
            }

            panel.SetActive(active);
        }
    }
}
