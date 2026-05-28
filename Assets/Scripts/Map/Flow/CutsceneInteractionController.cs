using System.Collections;
using UnityEngine;
using TOME.Data;
using TOME.Managers;

namespace TOME.Map
{
    /// <summary>대화 중 인터랙티브 컷신 처리. DialogueManager의 OnInteractionRequested를 받아
    /// 해당 클릭 오브젝트를 활성화하고, 사용자가 클릭하면 연출(낙서 표시/유령 움찔)을 재생한 뒤
    /// 짧은 대기 후 대화를 재개한다. Room_Hallway 등 맵 씬에 배치.</summary>
    public class CutsceneInteractionController : MonoBehaviour
    {
        [System.Serializable]
        public class Step
        {
            public DialogueTrigger trigger;
            [Tooltip("이 트리거에서 클릭을 받을 오브젝트(Collider2D 필요).")]
            public GameObject clickTarget;
            [Tooltip("클릭 시 켜질 연출 오브젝트(예: 벽 낙서 스프라이트). 없으면 생략.")]
            public GameObject revealOnClick;
            [Tooltip("클릭 시 트리거할 Animator(예: 유령 움찔). 없으면 생략.")]
            public Animator animator;
            public string animatorTrigger = "React";
            [Tooltip("연출 후 대화 재개까지 대기(초).")]
            public float resumeDelay = 1.0f;
            [Tooltip("clickTarget 위에 표시할 안내 하이라이트(선택).")]
            public GameObject hintHighlight;
        }

        [SerializeField] Step[] steps;
        [SerializeField] Camera worldCamera;

        Step _active;

        void Awake()
        {
            if (worldCamera == null) worldCamera = Camera.main;
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
            _active = FindStep(trigger);
            if (_active == null)
            {
                // 설정이 없으면 즉시 재개 (대화 멈춤 방지)
                Debug.LogWarning($"[Cutscene] No Step configured for trigger '{trigger}'. Resuming dialogue.");
                DialogueManager.I?.ResumeFromInteraction();
                return;
            }
            if (_active.hintHighlight) _active.hintHighlight.SetActive(true);
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
