using UnityEngine;
using UnityEngine.UI;
using Colosseum.Player;

namespace Colosseum.UI
{
    public class PlayerHPBar : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Image _fillImage;

        [Header("Settings")]
        [SerializeField] private Vector3 _offset = new Vector3(0f, 1.2f, 0f);
        [SerializeField] private float _maxHealth = 100f;

        private PlayerHealth _playerHealth;
        private Transform _target;
        private Camera _cam;

        public void Init(PlayerHealth health)
        {
            _playerHealth = health;
            _target = health.transform;
            _cam = Camera.main;
        }

        private void LateUpdate()
        {
            if (_playerHealth == null || _target == null)
            {
                Destroy(gameObject);
                return;
            }

            // 죽었으면 숨기기
            if (_playerHealth.IsDead)
            {
                _fillImage.transform.parent.gameObject.SetActive(false);
                return;
            }
            else
            {
                _fillImage.transform.parent.gameObject.SetActive(true);
            }

            // 월드 좌표 → 스크린 좌표로 변환
            if (_cam == null) _cam = Camera.main;
            if (_cam == null) return;

            Vector3 worldPos = _target.position + _offset;
            Vector3 screenPos = _cam.WorldToScreenPoint(worldPos);
            transform.position = screenPos;

            // HP 비율 업데이트
            float ratio = Mathf.Clamp01(_playerHealth.CurrentHealth / _maxHealth);
            _fillImage.fillAmount = ratio;

            // HP에 따라 색 변경
            if (ratio > 0.5f)
                _fillImage.color = Color.Lerp(Color.yellow, Color.green, (ratio - 0.5f) * 2f);
            else
                _fillImage.color = Color.Lerp(Color.red, Color.yellow, ratio * 2f);
        }
    }
}
