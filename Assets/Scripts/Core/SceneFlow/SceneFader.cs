using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

using TOME.GameFlow;
using TOME.Title;
namespace TOME.Core
{
    public class SceneFader : MonoBehaviour
    {
        public static SceneFader I { get; private set; }

        [SerializeField] CanvasGroup group;
        [Tooltip("한쪽(암전 또는 밝아짐)에 걸리는 시간(초).")]
        [Min(0f)] [SerializeField] float defaultDuration = 0.5f;
        [Tooltip("페이드 가속 곡선. 선형이면 시작·끝이 딱 끊겨 뚝 바뀌는 느낌이 난다.")]
        [SerializeField] AnimationCurve ease = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        [Tooltip("완전히 어두워진 뒤 씬을 로드하기까지 검은 화면을 유지할 시간(초). " +
                 "로딩 순간의 프레임 끊김을 검은 화면 뒤로 숨긴다.")]
        [Min(0f)] [SerializeField] float holdSeconds = 0.08f;
        [Tooltip("게임을 처음 켰을 때 검은 화면에서 밝아지며 시작할지. 끄면 첫 화면이 그냥 툭 나타난다.")]
        [SerializeField] bool fadeInOnLaunch = true;

        bool transitioning;

        void Awake()
        {
            // 컴포넌트만 지우면 씬마다 있는 검은 전체화면 캔버스가 그대로 쌓인다 → 오브젝트째 제거한다.
            if (I != null && I != this) { Destroy(gameObject); return; }
            I = this;
            if (transform.parent != null) transform.SetParent(null, true);
            DontDestroyOnLoad(gameObject);
            // 실행 직후엔 검은 화면에서 시작해 밝아진다(Start에서 FadeIn). 첫 화면이 툭 튀지 않게.
            if (group)
            {
                group.alpha = fadeInOnLaunch ? 1f : 0f;
                group.blocksRaycasts = fadeInOnLaunch;
            }
        }

        void Start()
        {
            if (fadeInOnLaunch && group) StartCoroutine(FadeIn());
        }

        /// <summary>페이드로 씬을 전환한다. 페이더가 없으면 경고를 남기고 즉시 로드한다 —
        /// 조용히 페이드 없이 넘어가면 "페이드가 어디 갔지"를 추적할 단서가 남지 않는다.</summary>
        public static void Go(string sceneName, float? fade = null)
        {
            if (I != null) { I.TransitionToScene(sceneName, fade); return; }
            Debug.LogWarning($"[SceneFader] 씬에 SceneFader가 없어 '{sceneName}' 전환을 페이드 없이 진행합니다.");
            SceneManager.LoadSceneAsync(sceneName);
        }

        void OnDestroy() { if (I == this) I = null; }

        /// 페이드아웃 → 씬 로드 → 페이드인을 영속 SceneFader가 호스팅한다.
        /// 호출자(BootstrapEntry/GameManager)가 씬 언로드로 파괴돼도 전환이 끊기지 않는다.
        public void TransitionToScene(string sceneName, float? fade = null)
        {
            if (transitioning) return;
            StartCoroutine(TransitionRoutine(sceneName, fade ?? defaultDuration));
        }

        IEnumerator TransitionRoutine(string sceneName, float dur)
        {
            transitioning = true;
            yield return Fade(0f, 1f, dur);
            // 완전히 어두워진 상태를 잠깐 유지 — 로드 히칭이 검은 화면 뒤에서 지나간다.
            if (holdSeconds > 0f) yield return new WaitForSecondsRealtime(holdSeconds);
            var op = SceneManager.LoadSceneAsync(sceneName);
            while (op != null && !op.isDone) yield return null;
            yield return null;              // 새 씬의 Start()가 한 번 돌고 나서 밝아진다(첫 프레임 팝 방지)
            yield return Fade(1f, 0f, dur);
            transitioning = false;
        }

        public IEnumerator FadeOut(float? d = null) => Fade(0f, 1f, d ?? defaultDuration);
        public IEnumerator FadeIn (float? d = null) => Fade(1f, 0f, d ?? defaultDuration);

        IEnumerator Fade(float from, float to, float dur)
        {
            if (!group) yield break;
            group.blocksRaycasts = true;
            group.alpha = from;                 // 시작값 명시 — 스파이크로 첫 프레임이 건너뛰는 문제 방지
            yield return null;                  // 전환 진입/로드 직후의 큰 deltaTime 프레임 1회 흘려보냄
            float t = 0f;
            if (dur <= 0f) dur = 0.0001f;
            while (t < 1f)
            {
                // 프레임 스파이크가 페이드를 한 번에 끝내지 않도록 per-frame 진행을 제한
                t += Mathf.Min(Time.unscaledDeltaTime, 0.05f) / dur;
                // 선형이 아니라 곡선으로 — 시작과 끝이 완만해져 부드럽게 넘어간다.
                group.alpha = Mathf.LerpUnclamped(from, to, ease.Evaluate(Mathf.Clamp01(t)));
                yield return null;
            }
            group.alpha = to;
            group.blocksRaycasts = to > 0.5f;
        }
    }
}
