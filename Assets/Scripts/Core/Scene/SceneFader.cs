using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace TOME.Core
{
    public class SceneFader : MonoBehaviour
    {
        public static SceneFader I { get; private set; }

        [SerializeField] CanvasGroup group;
        [SerializeField] float defaultDuration = 0.35f;

        void Awake()
        {
            if (I != null && I != this) { Destroy(gameObject); return; }
            I = this; DontDestroyOnLoad(gameObject);
            if (group) group.alpha = 0f;
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
