using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using TOME.Managers;

namespace TOME.Core
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

        [Header("Blackout (기획서 p2/p3: 화면 클릭시 암전 출력 1초)")]
        [Tooltip("암전용 전체화면 검은 이미지. 평소 비활성.")]
        [SerializeField] GameObject blackoutRoot;
        [Min(0f)] [SerializeField] float blackoutSeconds = 1f;
        [Tooltip("암전 시 함께 끌 타이틀 화면 요소들(로고·타이틀 이미지 등). " +
                 "기획서 p2 화면2는 아무것도 없는 완전 암전이다.")]
        [SerializeField] GameObject[] titleObjects;

        [Header("오늘의 할일 (기획서 p2: 새게임일 때만)")]
        [Tooltip("\"오늘의 할일은?\" 문구 + 나무판자 버튼을 담은 패널. 평소 비활성.")]
        [SerializeField] GameObject todayTaskPanel;
        [Tooltip("나무판자 버튼. 누르면 튜토리얼이 있는 첫 씬으로 넘어간다.")]
        [SerializeField] UnityEngine.UI.Button todayTaskButton;
        [Tooltip("창이 나타나고 사라지는 시간(초). 0이면 즉시(하드 컷).")]
        [Min(0f)] [SerializeField] float todayTaskFadeSeconds = 0.35f;
        [Tooltip("나타날 때 아래에서 떠오르는 거리(px). 0이면 제자리에서 알파만 변한다.")]
        [SerializeField] float todayTaskRiseDistance = 40f;
        [Tooltip("나타남·사라짐 가속 곡선.")]
        [SerializeField] AnimationCurve todayTaskFadeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        bool _transitioning;

        IEnumerator Start()
        {
            if (blackoutRoot)   blackoutRoot.SetActive(false);
            if (todayTaskPanel) todayTaskPanel.SetActive(false);

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

            // 기획서 p2/p3: 화면 클릭 후 암전 1초. 로고·타이틀 이미지까지 전부 걷어낸다.
            // SetActive 로 툭 끄면 하드 컷이 된다 → SceneFader 로 부드럽게 어두워진 뒤,
            // 검은 화면 뒤에서 타이틀을 걷고 암전 패널을 켠다.
            if (SceneFader.I != null) yield return SceneFader.I.FadeOut();

            if (touchToStartHint) touchToStartHint.SetActive(false);
            if (titleObjects != null)
                foreach (var go in titleObjects)
                    if (go != null) go.SetActive(false);
            if (blackoutRoot) blackoutRoot.SetActive(true);

            // 페이더를 걷어도 blackoutRoot 가 검은 화면을 유지하므로 화면 변화 없이 이어진다.
            // (걷지 않으면 페이더가 최상단이라 그 위에 띄울 '오늘의 할일' 패널이 가려진다.)
            if (SceneFader.I != null) yield return SceneFader.I.FadeIn();

            if (blackoutSeconds > 0f) yield return new WaitForSecondsRealtime(blackoutSeconds);

            // 기획서 p2: 새게임이면 암전 위에 "오늘의 할일은?" + 나무판자 버튼을 띄우고
            // 그 버튼을 눌러야 튜토리얼이 시작된다. 기존 기록이 있으면(p3) 바로 맵으로 간다.
            if (!HasSeenIntro())
                yield return WaitForTodayTaskRoutine();

            SceneFader.Go(firstScene);   // 페이더가 없으면 Go 가 경고를 남기고 즉시 로드한다
        }

        static bool HasSeenIntro()
            => SaveSystemManager.I != null && SaveSystemManager.I.SeenIntro;

        IEnumerator WaitForTodayTaskRoutine()
        {
            if (todayTaskPanel == null || todayTaskButton == null) yield break;

            bool pressed = false;
            void OnPressed() => pressed = true;

            // SetActive 로 툭 켜고 끄면 하드 컷이 된다 → 알파 + 살짝 떠오름으로 부드럽게.
            var group = todayTaskPanel.GetComponent<CanvasGroup>();
            if (group == null) group = todayTaskPanel.AddComponent<CanvasGroup>();
            var rect = todayTaskPanel.transform as RectTransform;
            Vector2 shown = rect != null ? rect.anchoredPosition : Vector2.zero;

            todayTaskPanel.SetActive(true);
            todayTaskButton.onClick.AddListener(OnPressed);
            yield return FadePanel(group, rect, shown, 0f, 1f);

            while (!pressed) yield return null;

            todayTaskButton.onClick.RemoveListener(OnPressed);
            yield return FadePanel(group, rect, shown, 1f, 0f);
            if (rect != null) rect.anchoredPosition = shown;   // 다음 표시를 위해 원위치 복구
            todayTaskPanel.SetActive(false);
        }

        /// <summary>패널을 알파 + 세로 이동으로 부드럽게 띄우거나 내린다.
        /// 사라지는 동안에는 입력을 막아 버튼이 두 번 눌리지 않게 한다.</summary>
        IEnumerator FadePanel(CanvasGroup group, RectTransform rect, Vector2 shown, float from, float to)
        {
            bool appearing = to > from;
            Vector2 hidden = shown + new Vector2(0f, -todayTaskRiseDistance);
            Vector2 p0 = appearing ? hidden : shown;
            Vector2 p1 = appearing ? shown  : hidden;

            group.alpha = from;
            group.blocksRaycasts = appearing;
            group.interactable   = appearing;

            float dur = Mathf.Max(todayTaskFadeSeconds, 0.0001f);
            float t = 0f;
            while (t < 1f)
            {
                // 프레임 스파이크가 연출을 한 번에 끝내지 않도록 per-frame 진행을 제한 (SceneFader와 동일)
                t += Mathf.Min(Time.unscaledDeltaTime, 0.05f) / dur;
                float k = todayTaskFadeCurve.Evaluate(Mathf.Clamp01(t));
                group.alpha = Mathf.LerpUnclamped(from, to, k);
                if (rect != null) rect.anchoredPosition = Vector2.LerpUnclamped(p0, p1, k);
                yield return null;
            }
            group.alpha = to;
            if (rect != null) rect.anchoredPosition = p1;
        }
    }
}
