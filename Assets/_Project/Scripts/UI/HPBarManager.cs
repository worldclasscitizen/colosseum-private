using UnityEngine;
using UnityEngine.UI;
using Colosseum.Player;

namespace Colosseum.UI
{
    public class HPBarManager : MonoBehaviour
    {
        [Header("HP Bar Settings")]
        [SerializeField] private Canvas _canvas;
        [SerializeField] private float _barWidth = 80f;
        [SerializeField] private float _barHeight = 10f;

        private PlayerHealth[] _trackedPlayers = new PlayerHealth[0];

        private void Update()
        {
            // 새로운 플레이어 감지
            var players = FindObjectsOfType<PlayerHealth>();
            if (players.Length != _trackedPlayers.Length)
            {
                foreach (var player in players)
                {
                    if (!HasHPBar(player))
                    {
                        CreateHPBar(player);
                    }
                }
                _trackedPlayers = players;
            }
        }

        private bool HasHPBar(PlayerHealth player)
        {
            var bars = GetComponentsInChildren<PlayerHPBar>();
            foreach (var bar in bars)
            {
                // bar의 타겟이 이 플레이어인지 확인
                var field = typeof(PlayerHPBar).GetField("_playerHealth",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (field != null)
                {
                    var target = field.GetValue(bar) as PlayerHealth;
                    if (target == player) return true;
                }
            }
            return false;
        }

        private void CreateHPBar(PlayerHealth player)
        {
            // HP Bar 루트
            GameObject barObj = new GameObject($"HPBar_{player.name}");
            barObj.transform.SetParent(_canvas.transform, false);

            var hpBar = barObj.AddComponent<PlayerHPBar>();

            // 배경 (어두운 바)
            GameObject bgObj = new GameObject("Background");
            bgObj.transform.SetParent(barObj.transform, false);
            var bgImage = bgObj.AddComponent<Image>();
            bgImage.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
            var bgRect = bgObj.GetComponent<RectTransform>();
            bgRect.sizeDelta = new Vector2(_barWidth, _barHeight);

            // 채우기 (HP 표시)
            GameObject fillObj = new GameObject("Fill");
            fillObj.transform.SetParent(bgObj.transform, false);
            var fillImage = fillObj.AddComponent<Image>();
            fillImage.color = Color.green;
            fillImage.type = Image.Type.Filled;
            fillImage.fillMethod = Image.FillMethod.Horizontal;
            fillImage.fillOrigin = 0; // Left
            fillImage.fillAmount = 1f;
            var fillRect = fillObj.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;

            // Fill Image를 PlayerHPBar에 연결
            var fillField = typeof(PlayerHPBar).GetField("_fillImage",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (fillField != null)
            {
                fillField.SetValue(hpBar, fillImage);
            }

            hpBar.Init(player);
            Debug.Log($"[Colosseum] HP Bar created for {player.name}");
        }
    }
}
