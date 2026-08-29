using System.Collections;
using UnityEngine;

namespace TOME.Tutorial
{
    /// <summary>화면을 어둡게 덮고, 지정 오브젝트 영역만 둥근 모서리 사각형으로 뚫어 남긴다.
    /// 오브젝트별 색을 만지지 않는 이유: 기획서 p4 예시가 대상 *주변 영역*(뒤 벽·바닥 포함)을
    /// 통째로 밝게 남기기 때문. 오브젝트 단위로 낮추면 대상만 밝고 뒤 바닥이 어두워져 오려낸 것처럼 보인다.
    /// 대상 없이 호출하면 스포트라이트 없는 균일 암전이 되어 나레이션 모드가 재사용한다.</summary>
    [RequireComponent(typeof(MeshRenderer), typeof(MeshFilter))]
    public class SpotlightDimmer : MonoBehaviour
    {
        [Tooltip("오버레이가 따라갈 카메라. 비우면 Camera.main.")]
        [SerializeField] Camera targetCamera;
        [Tooltip("RoundedSpotlight 셰이더를 쓰는 머티리얼.")]
        [SerializeField] Material overlayMaterial;
        [Tooltip("오버레이 정렬 순서. 맵 오브젝트보다 앞, 대화 UI보다 뒤.")]
        [SerializeField] int sortingOrder = 500;
        [Tooltip("스포트라이트 모서리 부드러움(월드 단위).")]
        [SerializeField] float edgeSoftness = 0.15f;
        [Tooltip("오버레이를 화면보다 얼마나 크게 잡을지. 1이면 딱 맞고, 여유를 두면 가장자리가 비지 않는다.")]
        [SerializeField] float overscan = 1.05f;
        [Tooltip("카메라 앞쪽으로 오버레이를 놓을 거리. 카메라 근평면보다는 멀어야 보인다.")]
        [SerializeField] float distanceFromCamera = 1f;

        static readonly int CenterId    = Shader.PropertyToID("_Center");
        static readonly int HalfSizeId  = Shader.PropertyToID("_HalfSize");
        static readonly int RadiusId    = Shader.PropertyToID("_Radius");
        static readonly int SoftnessId  = Shader.PropertyToID("_Softness");
        static readonly int DimAmountId = Shader.PropertyToID("_DimAmount");

        MeshRenderer _renderer;
        Material     _instance;   // 공유 머티리얼 오염 방지용 인스턴스
        float        _dimAmount;

        public bool IsDimmed => _dimAmount > 0.0001f;

        void Awake()
        {
            if (targetCamera == null) targetCamera = Camera.main;
            _renderer = GetComponent<MeshRenderer>();
            _renderer.sortingOrder = sortingOrder;

            if (overlayMaterial != null)
            {
                _instance = new Material(overlayMaterial);
                _renderer.material = _instance;
            }
            else
            {
                Debug.LogWarning("[Tutorial] SpotlightDimmer에 overlayMaterial이 없습니다. 암전이 표시되지 않습니다.");
            }

            SetDim(0f);
            SetSpotlight(Vector2.zero, Vector2.zero, 0f);
            _renderer.enabled = false;
        }

        void OnDestroy()
        {
            UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
            if (_instance != null) Destroy(_instance);
        }

        void OnEnable() => UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
        void OnDisable() => UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;

        // 스포트라이트 중심은 월드 좌표라 다른 씬에서는 의미가 없다.
        // 이 오브젝트가 어떤 이유로든 씬을 넘어 살아남으면(예: DontDestroyOnLoad 부모 아래) 화면이 어두운 채로 남으므로
        // 씬이 바뀌면 무조건 암전을 걷는다.
        void OnSceneLoaded(UnityEngine.SceneManagement.Scene s, UnityEngine.SceneManagement.LoadSceneMode m)
        {
            StopAllCoroutines();
            SetDim(0f);
            SetSpotlight(Vector2.zero, Vector2.zero, 0f);
            if (_renderer != null) _renderer.enabled = false;
        }

        // 카메라가 좌우로 움직여도(ScreenNavigator) 오버레이가 화면을 계속 덮게 한다.
        void LateUpdate()
        {
            if (_renderer == null || !_renderer.enabled) return;
            if (targetCamera == null || !targetCamera.orthographic) return;
            FitToCamera();
        }

        void FitToCamera()
        {
            var camT = targetCamera.transform;
            float h = targetCamera.orthographicSize * 2f;
            float w = h * targetCamera.aspect;
            transform.position   = new Vector3(camT.position.x, camT.position.y,
                                               camT.position.z + distanceFromCamera);
            transform.rotation   = camT.rotation;
            transform.localScale = new Vector3(w * overscan, h * overscan, 1f);
        }

        /// <summary>target을 감싸는 둥근 사각형만 남기고 암전한다.
        /// target이 null이면 스포트라이트 없이 화면 전체를 균일하게 어둡게 한다.</summary>
        public IEnumerator DimRoutine(GameObject target, float padding, float cornerRadius,
                                      float dimAmount, float duration)
        {
            if (_renderer == null) yield break;

            _renderer.enabled = true;
            FitToCamera();

            if (target != null && TryGetBounds(target, out Bounds b))
            {
                var half = new Vector2(b.extents.x + padding, b.extents.y + padding);
                SetSpotlight(b.center, half, cornerRadius);
            }
            else
            {
                SetSpotlight(Vector2.zero, Vector2.zero, 0f);
            }

            yield return FadeRoutine(_dimAmount, Mathf.Clamp01(dimAmount), duration);
        }

        /// <summary>대상 크기에 비례해 스포트라이트를 맞춘다.
        /// 여백·둥글기를 절대값(월드 단위)으로 주면 대상이 커지거나 작아질 때마다 다시 잡아야 하지만,
        /// 비율로 주면 크기가 제각각인 오브젝트에 같은 인상으로 자동으로 맞는다.</summary>
        /// <param name="paddingRatio">대상 반지름 대비 여백 비율. 0.1이면 10% 넓게, 음수면 대상보다 타이트하게.</param>
        /// <param name="cornerRatio">짧은 변 절반에 대한 모서리 둥글기 비율(0~1).</param>
        public IEnumerator DimRelativeRoutine(GameObject target, float paddingRatio, float cornerRatio,
                                              float dimAmount, float duration)
        {
            if (_renderer == null) yield break;

            _renderer.enabled = true;
            FitToCamera();

            if (target != null && TryGetBounds(target, out Bounds b))
            {
                var half = new Vector2(b.extents.x * (1f + paddingRatio),
                                       b.extents.y * (1f + paddingRatio));
                half.x = Mathf.Max(half.x, 0.01f);
                half.y = Mathf.Max(half.y, 0.01f);
                SetSpotlight(b.center, half, Mathf.Min(half.x, half.y) * Mathf.Clamp01(cornerRatio));
            }
            else
            {
                SetSpotlight(Vector2.zero, Vector2.zero, 0f);
            }

            yield return FadeRoutine(_dimAmount, Mathf.Clamp01(dimAmount), duration);
        }

        /// <summary>암전을 걷어낸다.</summary>
        public IEnumerator UndimRoutine(float duration)
        {
            if (_renderer == null) yield break;
            if (!IsDimmed) { _renderer.enabled = false; yield break; }

            yield return FadeRoutine(_dimAmount, 0f, duration);
            _renderer.enabled = false;
        }

        // 대상과 그 자식들을 모두 감싸는 바운즈. 여러 스프라이트로 된 오브젝트 대응.
        // 스프라이트는 Renderer.bounds 대신 타이트 메시(불투명 픽셀을 감싸는 폴리곤)를 쓴다 —
        // 투명 여백이 있는 그림은 Renderer.bounds가 실제로 보이는 것보다 커서 강조 사각형이 헐렁해진다.
        static bool TryGetBounds(GameObject go, out Bounds bounds)
        {
            bounds = default;
            bool any = false;

            foreach (var r in go.GetComponentsInChildren<Renderer>(true))
            {
                if (r == null) continue;
                Bounds b = (r is SpriteRenderer sr && TryGetTightBounds(sr, out Bounds tight)) ? tight : r.bounds;
                if (!any) { bounds = b; any = true; }
                else bounds.Encapsulate(b);
            }
            return any;
        }

        // 스프라이트 타이트 메시 정점의 월드 바운즈. 메시가 FullRect면 Renderer.bounds와 같아진다.
        static bool TryGetTightBounds(SpriteRenderer sr, out Bounds bounds)
        {
            bounds = default;
            if (sr.sprite == null) return false;
            var verts = sr.sprite.vertices;
            if (verts == null || verts.Length == 0) return false;

            var tf = sr.transform;
            bounds = new Bounds(tf.TransformPoint(verts[0]), Vector3.zero);
            for (int i = 1; i < verts.Length; i++) bounds.Encapsulate(tf.TransformPoint(verts[i]));
            return true;
        }

        void SetSpotlight(Vector2 center, Vector2 halfSize, float radius)
        {
            if (_instance == null) return;
            _instance.SetVector(CenterId,   new Vector4(center.x, center.y, 0f, 0f));
            _instance.SetVector(HalfSizeId, new Vector4(halfSize.x, halfSize.y, 0f, 0f));
            _instance.SetFloat(RadiusId,    radius);
            _instance.SetFloat(SoftnessId,  edgeSoftness);
        }

        void SetDim(float amount)
        {
            _dimAmount = amount;
            if (_instance != null) _instance.SetFloat(DimAmountId, amount);
        }

        IEnumerator FadeRoutine(float from, float to, float duration)
        {
            if (duration <= 0f) { SetDim(to); yield break; }
            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                SetDim(Mathf.Lerp(from, to, Mathf.Clamp01(t / duration)));
                yield return null;
            }
            SetDim(to);
        }
    }
}
