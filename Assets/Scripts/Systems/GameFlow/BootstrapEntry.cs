using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using TOME.Core;

namespace TOME.Systems
{
    /// <summary>타이틀(TOUCH TO START) 화면. 탭하면 컷신(카메라 좌측 팬→3초 홀드→페이드아웃) 후 첫 씬으로 전환.
    /// waitForTouch=false면 종전처럼 즉시 전환(테스트용).</summary>
    public class BootstrapEntry : MonoBehaviour
    {
        [SerializeField] string firstScene = "Map_Room";
        [SerializeField] bool   waitForTouch = true;
        [SerializeField] GameObject touchToStartHint;

        [Header("Intro Cutscene")]
        [SerializeField] Camera introCamera;          // 비우면 Camera.main 사용
        [SerializeField] float  panOffsetX  = -5f;    // 카메라 X 이동량(좌측 음수)
        [SerializeField] float  panDuration = 1.0f;
        [SerializeField] float  holdSeconds = 3.0f;   // 팬 후 정지 시간
        [SerializeField] AnimationCurve panCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        bool _transitioning;

        IEnumerator Start()
        {
            DialogueManager.I?.PreloadAll();
            yield return null;

            if (!waitForTouch) { Go(); yield break; }

            if (touchToStartHint) touchToStartHint.SetActive(true);
            while (!_transitioning)
            {
                if (Input.GetMouseButtonDown(0) || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began))
                    Go();
                yield return null;
            }
        }

        void Go()
        {
            if (_transitioning) return;
            _transitioning = true;
            StartCoroutine(IntroCutsceneRoutine());
        }

        IEnumerator IntroCutsceneRoutine()
        {
            var cam = introCamera != null ? introCamera : Camera.main;
            if (cam != null && (panOffsetX != 0f || panDuration > 0f))
            {
                Vector3 startPos = cam.transform.position;
                Vector3 endPos   = startPos + new Vector3(panOffsetX, 0f, 0f);
                float t = 0f;
                float dur = Mathf.Max(panDuration, 0.0001f);
                while (t < dur)
                {
                    t += Time.unscaledDeltaTime;
                    float k = panCurve.Evaluate(Mathf.Clamp01(t / dur));
                    cam.transform.position = Vector3.LerpUnclamped(startPos, endPos, k);
                    yield return null;
                }
                cam.transform.position = endPos;
            }
            if (holdSeconds > 0f) yield return new WaitForSecondsRealtime(holdSeconds);

            // 페이드 시작 직전에 라벨 숨기기 (컷신 동안엔 계속 표시)
            if (touchToStartHint) touchToStartHint.SetActive(false);

            if (SceneFader.I != null)
                SceneFader.I.TransitionToScene(firstScene);
            else
                SceneManager.LoadSceneAsync(firstScene);
        }
    }
}
