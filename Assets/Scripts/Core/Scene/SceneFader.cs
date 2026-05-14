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
            if (group) group.alpha = 0f;
        }

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
            float t = 0f;
            while (t < dur)
            {
                t += Time.unscaledDeltaTime;
                group.alpha = Mathf.Lerp(from, to, t / dur);
                yield return null;
            }
            group.alpha = to;
            group.blocksRaycasts = to > 0.5f;
        }
    }
}
