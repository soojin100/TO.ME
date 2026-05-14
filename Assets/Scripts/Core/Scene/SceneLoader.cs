using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TOME.Core
{
    public static class SceneLoader
    {
        public static IEnumerator LoadAsync(string sceneName, float fade = 0.35f)
        {
            if (SceneFader.I) yield return SceneFader.I.FadeOut(fade);
            var op = SceneManager.LoadSceneAsync(sceneName);
            while (!op.isDone) yield return null;
            if (SceneFader.I) yield return SceneFader.I.FadeIn(fade);
        }
    }
}
