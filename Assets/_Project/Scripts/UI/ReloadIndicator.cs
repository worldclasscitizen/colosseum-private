using UnityEngine;
using UnityEngine.UI;
using Colosseum.Weapon;

namespace Colosseum.UI
{
    /// <summary>
    /// 총(Gun) 오브젝트에 부착하는 재장전 인디케이터.
    /// 총 옆에 작은 흰색 원형 게이지가 재장전 중에만 나타나며,
    /// 진행률에 따라 12시 방향부터 시계 방향으로 차오른다.
    ///
    /// - Gun의 자식이므로 마우스 에임에 따라 총과 함께 이동
    /// - 총이 회전해도 링은 항상 정면(upright)을 유지 (counter-rotate)
    /// - 외부 스프라이트 에셋 불필요 (코드로 링 텍스처 생성)
    ///
    /// 사용법: Gun과 같은 GameObject에 AddComponent, 또는
    ///         Gun 오브젝트의 Inspector에서 추가.
    /// </summary>
    public class ReloadIndicator : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Gun _gun;
        [SerializeField] private Transform _muzzle;

        [Header("Position")]
        [Tooltip("총구(Muzzle) 기준 오프셋. 총의 로컬 좌표계 기준이 아니라, 월드 기준 고정 오프셋.")]
        [SerializeField] private Vector2 _offset = new Vector2(0f, 0.3f);

        [Header("Appearance")]
        [SerializeField] private float _size = 0.25f;
        [SerializeField] private float _ringThickness = 0.15f;
        [SerializeField] private Color _ringColor = Color.white;
        [SerializeField] private int _textureResolution = 128;

        private Canvas _canvas;
        private Image _ringImage;
        private RectTransform _canvasRect;
        private bool _initialized;

        private void Start()
        {
            if (_gun == null)
                _gun = GetComponent<Gun>();
            if (_gun == null)
                _gun = GetComponentInParent<Gun>();

            // Muzzle을 못 찾으면 Gun의 Transform 자체를 기준으로 사용
            if (_muzzle == null)
            {
                // Gun에서 _muzzle은 private이므로, 자식 중 "Muzzle"이란 이름의 Transform을 탐색
                _muzzle = transform.Find("Muzzle");
                if (_muzzle == null)
                    _muzzle = transform; // 폴백: Gun 위치 자체
            }

            CreateIndicator();
        }

        private void CreateIndicator()
        {
            // ── 월드 스페이스 Canvas (Gun의 자식이 아닌 씬 루트에 생성) ──
            // 이유: Gun이 회전하면 자식 Canvas도 같이 회전하기 때문에,
            //       독립 오브젝트로 만들고 LateUpdate에서 위치만 추적한다.
            var canvasObj = new GameObject("ReloadIndicatorCanvas");

            _canvas = canvasObj.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.WorldSpace;
            _canvas.sortingOrder = 5;

            _canvasRect = _canvas.GetComponent<RectTransform>();
            _canvasRect.sizeDelta = new Vector2(_size, _size);
            _canvasRect.localScale = Vector3.one;

            // ── 링 이미지 ──
            var ringObj = new GameObject("ReloadRing");
            ringObj.transform.SetParent(canvasObj.transform, false);

            _ringImage = ringObj.AddComponent<Image>();
            _ringImage.sprite = GenerateRingSprite(_textureResolution, _ringThickness);
            _ringImage.type = Image.Type.Filled;
            _ringImage.fillMethod = Image.FillMethod.Radial360;
            _ringImage.fillOrigin = (int)Image.Origin360.Top;
            _ringImage.fillClockwise = true;
            _ringImage.fillAmount = 0f;
            _ringImage.color = _ringColor;
            _ringImage.raycastTarget = false;

            var ringRect = ringObj.GetComponent<RectTransform>();
            ringRect.anchorMin = Vector2.zero;
            ringRect.anchorMax = Vector2.one;
            ringRect.offsetMin = Vector2.zero;
            ringRect.offsetMax = Vector2.zero;

            // 처음에는 숨김
            canvasObj.SetActive(false);
            _initialized = true;
        }

        private void LateUpdate()
        {
            if (!_initialized || _gun == null) return;

            if (_gun.IsReloading)
            {
                if (!_canvas.gameObject.activeSelf)
                    _canvas.gameObject.SetActive(true);

                // 총구 위치 + 월드 고정 오프셋 → 항상 총 위쪽에 표시
                Vector3 anchorPos = _muzzle.position;
                _canvasRect.position = new Vector3(
                    anchorPos.x + _offset.x,
                    anchorPos.y + _offset.y,
                    anchorPos.z
                );

                // 회전은 항상 정면 고정 (총이 회전해도 링은 upright)
                _canvasRect.rotation = Quaternion.identity;

                _ringImage.fillAmount = _gun.ReloadProgress;
            }
            else
            {
                if (_canvas.gameObject.activeSelf)
                    _canvas.gameObject.SetActive(false);
            }
        }

        private void OnDestroy()
        {
            // 독립 오브젝트이므로 Gun이 파괴될 때 같이 정리
            if (_canvas != null)
                Destroy(_canvas.gameObject);
        }

        /// <summary>
        /// 코드로 흰색 링(도넛) 텍스처를 생성하여 Sprite로 반환.
        /// 외부 에셋 없이 동작한다.
        /// </summary>
        private Sprite GenerateRingSprite(int resolution, float thickness)
        {
            var tex = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;

            float center = resolution * 0.5f;
            float outerRadius = center;
            float innerRadius = outerRadius * (1f - thickness);
            float edgeSoftness = 1.5f;

            var pixels = new Color32[resolution * resolution];

            for (int y = 0; y < resolution; y++)
            {
                for (int x = 0; x < resolution; x++)
                {
                    float dx = x - center + 0.5f;
                    float dy = y - center + 0.5f;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);

                    float outerAlpha = Mathf.Clamp01((outerRadius - dist) / edgeSoftness);
                    float innerAlpha = Mathf.Clamp01((dist - innerRadius) / edgeSoftness);

                    byte a = (byte)(outerAlpha * innerAlpha * 255);
                    pixels[y * resolution + x] = new Color32(255, 255, 255, a);
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply();

            return Sprite.Create(
                tex,
                new Rect(0, 0, resolution, resolution),
                new Vector2(0.5f, 0.5f),
                resolution
            );
        }
    }
}
