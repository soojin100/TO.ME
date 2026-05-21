using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TOME.Core;
using TOME.Data;
using TOME.Utils;

namespace TOME.Managers
{
    /// <summary>UI 측 Advance() 호출로 다음 줄 진행. 본 적 있으면 TryPlay false.
    /// {name} 토큰은 플레이어 이름으로 치환, 챕터별 히스토리는 다시보기에 사용.</summary>
    public class DialogueManager : MonoBehaviour
    {
        public static DialogueManager I { get; private set; }

        [SerializeField] TextAsset dialogueCsv;
        Dictionary<string, DialogueEntry> table;

        public event Action<DialogueEntry> OnLine;
        public event Action OnEnd;
        /// <summary>이름 입력 트리거 발생. UI가 팝업을 띄우고 SubmitName으로 재개.</summary>
        public event Action OnNameInputRequested;
        /// <summary>전투 시작 트리거 발생. 대사 종료 후 튜토리얼 전투로 진입.</summary>
        public event Action OnBattleStartRequested;

        public bool IsPlaying { get; private set; }

        // 현재 챕터의 지난 대사 (다시보기용). 챕터 경계에서 초기화.
        readonly List<DialogueEntry> _history = new(32);
        public IReadOnlyList<DialogueEntry> History => _history;

        bool _advance;
        bool _skip;
        bool _resume;   // 트리거 대기 해제

        void Awake()
        {
            if (I != null && I != this) { Destroy(gameObject); return; }
            I = this; DontDestroyOnLoad(gameObject);
        }

        void OnDestroy() { if (I == this) I = null; }

        public void PreloadAll() => table = CsvImporter.LoadDialogue(dialogueCsv);

        public bool TryPlay(string startId)
        {
            if (IsPlaying) return false;
            if (string.IsNullOrEmpty(startId) || table == null) return false;
            if (SaveSystemManager.I != null && SaveSystemManager.I.HasSeenDialogue(startId)) return false;
            StartCoroutine(Run(startId));
            return true;
        }

        /// <summary>UI 클릭/탭 시 호출.</summary>
        public void Advance() { _advance = true; }

        /// <summary>스킵 버튼: 현재 대사 시퀀스를 즉시 종료.</summary>
        public void SkipAll()
        {
            if (!IsPlaying) return;
            _skip = true;
            _resume = true; // 트리거 대기 중이어도 풀어준다
        }

        /// <summary>이름 입력 팝업 확정 시 호출. 저장 후 대사 재개.</summary>
        public void SubmitName(string name)
        {
            SaveSystemManager.I?.SetPlayerName(name);
            _resume = true;
        }

        IEnumerator Run(string startId)
        {
            IsPlaying = true;
            _skip = false;
            _history.Clear();
            string cur = startId;
            string chapter = null;

            while (!_skip && !string.IsNullOrEmpty(cur) && table.TryGetValue(cur, out var raw))
            {
                // 챕터가 바뀌면 다시보기 히스토리 초기화
                if (chapter != null && raw.chapter != chapter) _history.Clear();
                chapter = raw.chapter;

                var e = raw;
                e.speaker = Substitute(e.speaker);
                e.text    = Substitute(e.text);
                _history.Add(e);

                OnLine?.Invoke(e);
                _advance = false;
                while (!_advance && !_skip) yield return null;

                if (!_skip && e.trigger != DialogueTrigger.None)
                {
                    yield return HandleTrigger(e.trigger);
                    if (e.trigger == DialogueTrigger.StartBattle) break;
                }

                cur = e.next;
            }

            IsPlaying = false;
            _skip = false;
            SaveSystemManager.I?.MarkDialogueSeen(startId);
            OnEnd?.Invoke();
        }

        IEnumerator HandleTrigger(DialogueTrigger trigger)
        {
            switch (trigger)
            {
                case DialogueTrigger.NameInput:
                    _resume = false;
                    OnNameInputRequested?.Invoke();
                    while (!_resume && !_skip) yield return null;
                    _resume = false;
                    break;
                case DialogueTrigger.StartBattle:
                    OnBattleStartRequested?.Invoke();
                    break;
            }
        }

        string Substitute(string text)
        {
            if (string.IsNullOrEmpty(text) || text.IndexOf('{') < 0) return text;
            string name = SaveSystemManager.I != null ? SaveSystemManager.I.PlayerName : "제임스";
            return text.Replace("{name}", name);
        }
    }
}
