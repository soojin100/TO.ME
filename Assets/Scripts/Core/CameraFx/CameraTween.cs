using System.Collections;
using UnityEngine;

using TOME.Cutscene;
using TOME.Tutorial;
namespace TOME.Core
{
    /// <summary>카메라 이동·줌 보간 공용 루틴. 컷신 계열(TutorialCutsceneController,
    /// CutsceneInteractionController)이 같은 SmoothStep 팬줌을 공유한다.
    /// unscaled time 기준이라 일시정지(timeScale=0) 중에도 연출이 진행된다.</summary>
    public static class CameraTween
    {
        public static IEnumerator PanZoom(Camera cam, Vector3 fromPos, float fromSize,
                                          Vector3 toPos, float toSize, float duration)
        {
            if (cam == null) yield break;
            if (duration <= 0f)
            {
                cam.transform.position = toPos;
                cam.orthographicSize   = toSize;
                yield break;
            }

            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / duration));
                cam.transform.position = Vector3.Lerp(fromPos, toPos, k);
                cam.orthographicSize   = Mathf.Lerp(fromSize, toSize, k);
                yield return null;
            }
            cam.transform.position = toPos;
            cam.orthographicSize   = toSize;
        }
    }
}
