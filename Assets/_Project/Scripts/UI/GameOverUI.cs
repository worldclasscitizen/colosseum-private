using UnityEngine;
using TMPro;
using Colosseum.Game;

namespace Colosseum.UI
{
    public class GameOverUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject _gameOverPanel;
        [SerializeField] private TextMeshProUGUI _winnerText;

        private RoomManager _roomManager;

        private void Start()
        {
            _gameOverPanel.SetActive(false);
        }

        /// <summary>
        /// RoomManager에서 승리 시 호출
        /// </summary>
        public void ShowGameOver(string winnerName)
        {
            _gameOverPanel.SetActive(true);
            _winnerText.text = $"{winnerName} Wins!";
            Debug.Log($"[Colosseum] Game Over UI shown: {winnerName}");
        }
    }
}
