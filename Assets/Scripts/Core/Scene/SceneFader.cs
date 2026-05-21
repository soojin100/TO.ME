using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TOME.Core
{
    public class SceneFader : MonoBehaviour
    {
        public static SceneFader I { get; private set; }

        [SerializeField] CanvasGroup group;
        [SerializeField] float defaultDuration = 0.35f;

        bool transitioning;

        void Awake()
        {
            if (I != null && I != this) { Destroy(gameObject); return; }
            I = this; DontDestroyOnLoad(gameObject);
            if (group) { group.alpha = 0f; group.blocksRaycasts = false; }
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
            var op = SceneManager.LoadSceneAsync(sceneName);
            while (op != null && !op.isDone) yield return null;
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
                group.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(t));
                yield return null;
            }
            group.alpha = to;
            group.blocksRaycasts = to > 0.5f;
        }
    }
}
