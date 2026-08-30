using System.Collections;
using UnityEngine;
using TOME.Core;
using TOME.Dialogue;
using TOME.Map;
namespace TOME.Cutscene
{
    /// <summary>대화 중 인터랙티브/연출 컷신 처리. DialogueManager의 OnInteractionRequested를 받아
    /// (선택) 대상 섹션으로 카메라를 이동시킨 뒤, 두 가지 모드 중 하나로 진행한다.
    ///  - 포커스 모드(focusTarget 지정): 오브젝트로 카메라를 줌인 → 잠시 보여줌 → 줌아웃(이동했던 섹션에 머무름) → 대화 재개.
    ///  - 클릭 모드(focusTarget 비움): 클릭 안내 화살표 표시 → 사용자가 clickTarget 클릭 → 연출 후 대화 재개.
    /// Room_* 맵 씬의 Managers에 배치.</summary>
    public class CutsceneInteractionController : MonoBehaviour
    {
        [System.Serializable]
        public class Step
        {
            public DialogueTrigger trigger;
            [Tooltip("클릭 안내 전 카메라를 이동시킬 ScrollSections 섹션 인덱스. -1이면 이동 안 함(현재 위치 유지).")]
            public int sectionIndex = -1;

            [Header("포커스 모드 (지정 시 클릭 없이 카메라 줌인 연출)")]
            [Tooltip("줌인해서 보여줄 대상 Transform. 지정하면 클릭 모드 대신 카메라 포커스 연출로 동작.")]
            public Transform focusTarget;
            [Tooltip("줌인 시 카메라 orthographicSize(작을수록 확대). 보통 4 정도.")]
            public float zoomOrthoSize = 4f;
            [Tooltip("줌인 중심 보정(월드 단위). 대상이 화면 가장자리/방 밖에 걸릴 때 카메라 중심을 살짝 옮겨 빈 영역(남색 배경) 노출을 막는다. 예: 바닥 근처 오브젝트는 y를 + 로.")]
            public Vector2 focusOffset = Vector2.zero;
            [Tooltip("줌인 상태로 대상을 보여주는 시간(초).")]
            public float holdSeconds = 1.5f;
            [Tooltip("줌인/줌아웃 각 구간 소요 시간(초).")]
            public float panZoomDuration = 0.4f;

            [Header("클릭 모드 (focusTarget 비웠을 때)")]
            [Tooltip("이 트리거에서 클릭을 받을 오브젝트(Collider2D 필요).")]
            public GameObject clickTarget;
            [Tooltip("클릭 시 켜질 연출 오브젝트(예: 벽 낙서 스프라이트). 없으면 생략.")]
            public GameObject revealOnClick;
            [Tooltip("클릭 시 트리거할 Animator. 없으면 생략.")]
            public Animator animator;
            public string animatorTrigger = "React";
            [Tooltip("연출 후 대화 재개까지 대기(초). 0이면 클릭 즉시 다음 대화.")]
            public float resumeDelay = 0f;
            [Tooltip("clickTarget 위에 표시할 클릭 안내 화살표/하이라이트(선택).")]
            public GameObject hintHighlight;

            [Header("스포트라이트 암전 (기획서 p6 — 강조 대상만 밝게)")]
            [Tooltip("true면 clickTarget만 밝게 남기고 주변을 암전한다(씬에 SpotlightDimmer 필요). 밝은 영역(대상)을 클릭해야 진행되고, 클릭하면 암전이 걷힌다.")]
            public bool spotlightDim;
            [Tooltip("암전 강도(0~1).")]
            [Range(0f, 1f)] public float dimAmount = 0.7f;
            [Tooltip("대상 크기 대비 스포트라이트 여백 비율. 0.25면 대상보다 25% 넓게 남긴다.")]
            public float spotlightPaddingRatio = 0.25f;
        }

        [SerializeField] Step[] steps;
        [SerializeField] Camera worldCamera;
        [Tooltip("스포트라이트 암전용. 비우면 씬에서 자동 탐색. spotlightDim 스텝이 없으면 없어도 된다.")]
        [SerializeField] SpotlightDimmer dimmer;

        Step _active;   // 클릭 모드에서 클릭 대기 중인 step

        void Awake()
        {
            if (worldCamera == null) worldCamera = Camera.main;
            if (dimmer == null) dimmer = FindAnyObjectByType<SpotlightDimmer>();
            // 시작 시 모든 reveal/hint 비활성
            if (steps != null)
                foreach (var s in steps)
                {
                    if (s.revealOnClick) s.revealOnClick.SetActive(false);
                    if (s.hintHighlight) s.hintHighlight.SetActive(false);
                }
        }

        void OnEnable()
        {
            if (DialogueManager.I != null) DialogueManager.I.OnInteractionRequested += OnInteraction;
        }

        void OnDisable()
        {
            if (DialogueManager.I != null) DialogueManager.I.OnInteractionRequested -= OnInteraction;
        }

        void OnInteraction(DialogueTrigger trigger)
        {
            // 이 컨트롤러는 씬별 오브젝트(Cutscenes 루트 등)에 배치되어 씬 수명과 함께 생성/파괴된다.
            // → 영속 Managers에 두지 말 것(참조 dangling + 중복 처리 발생). 씬당 단일 인스턴스가 처리.
            var step = FindStep(trigger);
            if (step == null)
            {
                // 설정이 없으면 즉시 재개 (대화 멈춤 방지)
                Debug.LogWarning($"[Cutscene] No Step configured for trigger '{trigger}'. Resuming dialogue.");
                DialogueManager.I?.ResumeFromInteraction();
                return;
            }
            StartCoroutine(BeginStep(step));
        }

        // 섹션 이동(있으면 완료까지 대기) → 포커스 모드면 줌 연출, 클릭 모드면 화살표 표시 후 클릭 대기.
        IEnumerator BeginStep(Step step)
        {
            if (step.sectionIndex >= 0 && ScreenNavigator.Instance != null)
            {
                // 구역 수는 화면 비율·맵 폭에 따라 런타임에 정해진다. 예전에 잡아 둔 인덱스가 범위를 넘으면
                // 이동이 조용히 무시되므로, 마지막 구역으로 당겨 주고 무엇이 어긋났는지 남긴다.
                int last = ScreenNavigator.Instance.SectionCount - 1;
                int idx  = step.sectionIndex;
                if (idx > last)
                {
                    Debug.LogWarning($"[Cutscene] '{step.trigger}' 의 sectionIndex {idx} 가 구역 수({last + 1})를 넘습니다. " +
                                     $"{last} 번 구역으로 대신 이동합니다.", this);
                    idx = last;
                }
                var move = ScreenNavigator.Instance.MoveToSection(idx);
                if (move != null) yield return move;   // 이미 같은 섹션이면 null → 즉시 진행
            }

            if (step.focusTarget != null && worldCamera != null)
            {
                yield return FocusRoutine(step);
                DialogueManager.I?.ResumeFromInteraction();
            }
            else
            {
                // 클릭 대상이 없으면 이 스텝은 영원히 끝나지 않는다. 대화는 멈춘 채 대사창까지 닫혀 있어
                // 화면상 아무것도 반응하지 않는 상태가 된다 → 설정이 빈 경우는 그냥 재개한다.
                if (step.clickTarget == null)
                {
                    Debug.LogWarning($"[Cutscene] '{step.trigger}' 스텝에 focusTarget/clickTarget 이 모두 비어 있어 " +
                                     "진행할 수 없습니다. 대화를 재개합니다.", this);
                    DialogueManager.I?.ResumeFromInteraction();
                    yield break;
                }
                // 강조 대상만 밝게 남기는 암전(기획서 p6). 클릭 판정은 원래 clickTarget만 통과하므로
                // "어두워지지 않은 공간을 눌러야 진행"이 함께 성립한다.
                if (step.spotlightDim)
                {
                    if (dimmer != null)
                        yield return dimmer.DimRelativeRoutine(step.clickTarget, step.spotlightPaddingRatio,
                                                               0.5f, step.dimAmount, 0.3f);
                    else
                        Debug.LogWarning($"[Cutscene] '{step.trigger}' 스텝이 spotlightDim을 켰지만 씬에 SpotlightDimmer가 없습니다. 암전 없이 진행합니다.", this);
                }
                if (step.hintHighlight) step.hintHighlight.SetActive(true);
                _active = step;   // 이동 완료 후에만 클릭 받기
            }
        }

        // 카메라 포커스 연출: 현재(섹션 중앙) → 대상으로 줌인 → 홀드 → 다시 섹션 중앙으로 줌아웃.
        // 줌아웃 복귀 지점이 "이동해 온 섹션"이라 원래 있던 섹션으로 되돌아가지 않는다.
        IEnumerator FocusRoutine(Step s)
        {
            var camT = worldCamera.transform;
            Vector3 basePos  = camT.position;                 // 섹션 중앙(팬 완료 위치)
            float   baseSize = worldCamera.orthographicSize;
            Vector3 focusPos = new(s.focusTarget.position.x + s.focusOffset.x,
                                   s.focusTarget.position.y + s.focusOffset.y, basePos.z);

            yield return CameraTween.PanZoom(worldCamera, basePos, baseSize,
                                             focusPos, s.zoomOrthoSize, s.panZoomDuration);            // 줌인
            if (s.holdSeconds > 0f) yield return new WaitForSecondsRealtime(s.holdSeconds);            // 홀드
            yield return CameraTween.PanZoom(worldCamera, focusPos, s.zoomOrthoSize,
                                             basePos, baseSize, s.panZoomDuration);                    // 줌아웃(섹션에 머무름)
        }

        Step FindStep(DialogueTrigger trigger)
        {
            if (steps == null) return null;
            foreach (var s in steps) if (s != null && s.trigger == trigger) return s;
            return null;
        }

        void Update()
        {
            if (_active == null) return;
            if (!Input.GetMouseButtonDown(0)) return;
            if (worldCamera == null) return;

            Vector3 wp = worldCamera.ScreenToWorldPoint(Input.mousePosition);
            var hit = Physics2D.OverlapPoint(new Vector2(wp.x, wp.y));
            if (hit != null && _active.clickTarget != null &&
                (hit.gameObject == _active.clickTarget || hit.transform.IsChildOf(_active.clickTarget.transform)))
            {
                PlayReveal(_active);
            }
        }

        void PlayReveal(Step s)
        {
            if (s.hintHighlight) s.hintHighlight.SetActive(false);
            if (s.revealOnClick) s.revealOnClick.SetActive(true);
            if (s.animator && !string.IsNullOrEmpty(s.animatorTrigger)) s.animator.SetTrigger(s.animatorTrigger);
            if (s.spotlightDim && dimmer != null) StartCoroutine(dimmer.UndimRoutine(0.3f));
            _active = null;
            StartCoroutine(ResumeAfter(s.resumeDelay));
        }

        IEnumerator ResumeAfter(float delay)
        {
            if (delay > 0f) yield return new WaitForSecondsRealtime(delay);
            DialogueManager.I?.ResumeFromInteraction();
        }
    }
}
