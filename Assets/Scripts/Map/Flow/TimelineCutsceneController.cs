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
