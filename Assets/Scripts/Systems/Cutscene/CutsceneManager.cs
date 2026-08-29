using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace TOME.Systems
{
    public class CutsceneManager : MonoBehaviour
    {
        public static CutsceneManager I { get; private set; }

        [Header("컷신")]
        [SerializeField] GameObject cutsceneRoot;
        [SerializeField] Animator animator;

        [Header("대사 UI")]
        [SerializeField] GameObject dialogueRoot;
        [SerializeField] TMP_Text speakerLabel;
        [SerializeField] TMP_Text dialogueText;
        [SerializeField] Button advanceButton;

        [Serializable]
        public struct CutsceneDialogueLine
        {
            public string speaker;
            [TextArea] public string text;
            public string stateName;   
        }

        [Serializable]
        public struct CutsceneEntry
        {
            public string id;
            public string stateName;
            public CutsceneDialogueLine[] lines;
        }

        [SerializeField] CutsceneEntry[] entries;

        public event Action OnFinished;

        readonly HashSet<string> _seenIds = new HashSet<string>();

        void Awake()
        {
            if (I != null && I != this) { Destroy(gameObject); return; }
            I = this;
            if (cutsceneRoot) cutsceneRoot.SetActive(false);
            if (dialogueRoot) dialogueRoot.SetActive(false);
        }

        void OnDestroy() { if (I == this) I = null; }

        public bool TryPlay(string id)
        {
            Debug.Log($"[Cutscene] TryPlay 호출: {id}");
            if (string.IsNullOrEmpty(id)) { Debug.Log("[Cutscene] id 비어있음"); return false; }
            if (_seenIds.Contains(id)) { Debug.Log("[Cutscene] 이미 본 컷신"); return false; }

            CutsceneEntry? found = null;
            foreach (var e in entries)
                if (e.id == id) { found = e; break; }

            if (found == null) { Debug.Log($"[Cutscene] entries에서 {id} 못 찾음"); return false; }

            _seenIds.Add(id);
            StartCoroutine(PlayRoutine(found.Value));
            return true;
        }

        IEnumerator PlayRoutine(CutsceneEntry entry)
        {
            Debug.Log($"[Cutscene] PlayRoutine 시작: {entry.id}");
           
            CombatManager.I?.Pause();

            if (entry.lines != null && entry.lines.Length > 0 && dialogueRoot != null)
            {
                Debug.Log($"[Cutscene] 대사 {entry.lines.Length}줄 출력 시작");
                dialogueRoot.SetActive(true);

                foreach (var line in entry.lines)
                {
                    // 대사가 있으면 출력 후 탭 대기
                    if (!string.IsNullOrEmpty(line.text))
                    {
                        if (speakerLabel) speakerLabel.text = line.speaker;
                        if (dialogueText) dialogueText.text = line.text;

                        bool advanced = false;
                        if (advanceButton)
                            advanceButton.onClick.AddListener(() => advanced = true);

                        while (!advanced) yield return null;

                        if (advanceButton) advanceButton.onClick.RemoveAllListeners();
                    }

                    // 애니메이션이 있으면 재생
                    if (!string.IsNullOrEmpty(line.stateName) && cutsceneRoot && animator)
                    {
                        dialogueRoot.SetActive(false);
                        cutsceneRoot.SetActive(true);

                        animator.Play(line.stateName, 0, 0f);
                        yield return null;

                        while (animator.GetCurrentAnimatorStateInfo(0).normalizedTime < 1f
                               && !animator.IsInTransition(0))
                            yield return null;

                        cutsceneRoot.SetActive(false);
                        dialogueRoot.SetActive(true);
                    }
                }

                dialogueRoot.SetActive(false);
            }

            CombatManager.I?.Resume();
            OnFinished?.Invoke();
            OnFinished = null;
        }
    }
}