using UnityEngine;
using UnityEngine.Playables;
using TOME.Data;
using TOME.Managers;

namespace TOME.Map
{
    /// <summary>Timeline 기반 컷신 처리. DialogueManager.OnInteractionRequested 받으면
    /// 매핑된 PlayableDirector를 재생하고, Timeline 종료(stopped) 시 대사를 재개한다.
    /// CutsceneInteractionController(인터랙티브 클릭형)와 동일 흐름이지만 연출은 Timeline에 위임.</summary>
    public class TimelineCutsceneController : MonoBehaviour
    {
        [System.Serializable]
        public class Entry
        {
            public DialogueTrigger trigger;
            public PlayableDirector director;
        }

        [SerializeField] Entry[] entries;

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
            // 영속 매니저 GO와 함께 DDOL 씬에 살아남은 stale 인스턴스는 무시. 활성 씬의 인스턴스가 처리한다.
            // (이전 씬의 director 참조가 destroyed라 즉시 ResumeFromInteraction을 호출해 컷신을 끊는 버그 차단)
            if (gameObject.scene != UnityEngine.SceneManagement.SceneManager.GetActiveScene()) return;

            var e = FindEntry(trigger);
            if (e == null || e.director == null)
            {
                Debug.LogWarning($"[TimelineCutscene] No director configured for trigger '{trigger}'. Resuming.");
                DialogueManager.I?.ResumeFromInteraction();
                return;
            }
            e.director.stopped += OnDirectorStopped;
            e.director.Play();
        }

        Entry FindEntry(DialogueTrigger trig)
        {
            if (entries == null) return null;
            foreach (var e in entries) if (e != null && e.trigger == trig) return e;
            return null;
        }

        void OnDirectorStopped(PlayableDirector d)
        {
            d.stopped -= OnDirectorStopped;
            DialogueManager.I?.ResumeFromInteraction();
        }
    }
}
