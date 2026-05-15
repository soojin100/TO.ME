using UnityEngine;
using UnityEngine.EventSystems;

namespace Interactables
{
    [DisallowMultipleComponent]
    public class SpriteHighlight : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("외곽선 Material")]
        [SerializeField] private Material outlineMaterial;

        [Header("외곽선 설정")]
        [SerializeField] private Color outlineColor = new Color(1f, 1f, 0f, 1f);
        [SerializeField] private float outlineThickness = 4f;

        private SpriteRenderer _spriteRenderer;
        private Material _instanceMaterial;
        private Material _defaultMaterial;
        private static readonly int OutlineEnabled = Shader.PropertyToID("_OutlineEnabled");
        private static readonly int OutlineColor = Shader.PropertyToID("_OutlineColor");
        private static readonly int OutlineThickness = Shader.PropertyToID("_OutlineThickness");

        private void Awake()
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
            if (_spriteRenderer == null || outlineMaterial == null) return;

            _defaultMaterial = _spriteRenderer.material;
            _instanceMaterial = new Material(outlineMaterial);
            _instanceMaterial.SetColor(OutlineColor, outlineColor);
            _instanceMaterial.SetFloat(OutlineThickness, outlineThickness);
            _instanceMaterial.SetFloat(OutlineEnabled, 0f);
            // SpriteRenderer 가 머터리얼 swap 시점에 따라 _MainTex 자동 바인딩을 누락하는
            // 케이스가 있어, 스프라이트 텍스처를 명시적으로 셋업.
            BindSpriteTexture();
        }

        private void BindSpriteTexture()
        {
            if (_instanceMaterial == null || _spriteRenderer == null) return;
            var sprite = _spriteRenderer.sprite;
            if (sprite != null && sprite.texture != null)
                _instanceMaterial.mainTexture = sprite.texture;
        }

        private bool _canHighlight = true;

        public void SetHighlightAllowed(bool allowed)
        {
            _canHighlight = allowed;
            if (!allowed && _defaultMaterial != null)
                _spriteRenderer.material = _defaultMaterial;
        }

        public void ShowHighlight()
        {
            if (_instanceMaterial == null) return;
            BindSpriteTexture();
            _spriteRenderer.material = _instanceMaterial;
            _instanceMaterial.SetFloat(OutlineEnabled, 1f);
        }

        public void HideHighlight()
        {
            if (_defaultMaterial == null) return;
            _spriteRenderer.material = _defaultMaterial;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!_canHighlight) return;
            // 폰 열림 여부는 ClickBlocker 오브젝트가 raycast로 흡수해 처리. 여기선 별도 체크 불필요.
            if (_instanceMaterial == null) return;
            BindSpriteTexture();
            _spriteRenderer.material = _instanceMaterial;
            _instanceMaterial.SetFloat(OutlineEnabled, 1f);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (_defaultMaterial == null) return;
            _spriteRenderer.material = _defaultMaterial;
        }

        private void OnDestroy()
        {
            if (_instanceMaterial != null) Destroy(_instanceMaterial);
        }
    }
}