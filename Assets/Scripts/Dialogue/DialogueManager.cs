using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TOME.Cutscene;
using TOME.Save;
namespace TOME.Dialogue
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
        /// <summary>인터랙티브 컷신 트리거 발생. 맵의 클릭 오브젝트 연출 후 ResumeFromInteraction으로 재개.</summary>
        public event Action<DialogueTrigger> OnInteractionRequested;

        public bool IsPlaying { get; private set; }

        // 현재 챕터의 지난 대사 (다시보기용). 챕터 경계에서 초기화.
        readonly List<DialogueEntry> _history = new(32);
        public IReadOnlyList<DialogueEntry> History => _history;

        bool _advance;
        bool _fastForward;   // 넘어가기: 대화 줄만 빠르게 진행. 트리거(컷신/전투)는 정상 실행.
        bool _resume;        // 트리거 대기 해제

        void Awake()
        {
            if (I != null && I != this) { Destroy(this); return; }
            I = this;
            if (transform.parent != null) transform.SetParent(null, true);
            DontDestroyOnLoad(gameObject);
            if (table == null && dialogueCsv != null) PreloadAll();
        }

        void OnDestroy() { if (I == this) I = null; }

        public void PreloadAll() => table = DialogueCsvImporter.LoadDialogue(dialogueCsv);

        public bool TryPlay(string startId)
        {
            Debug.Log($"[Dialogue] TryPlay 호출: {startId}, IsPlaying={IsPlaying}, table={table?.Count}");

            if (IsPlaying) { Debug.Log("[Dialogue] 이미 재생 중"); return false; }
            if (string.IsNullOrEmpty(startId) || table == null) { Debug.Log("[Dialogue] startId 비었거나 table null"); return false; }
            if (SaveSystemManager.I != null && SaveSystemManager.I.HasSeenDialogue(startId)) { Debug.Log("[Dialogue] 이미 본 대화"); return false; }

            StartCoroutine(Run(startId));
            return true;
        }

        /// <summary>UI 클릭/탭 시 호출.</summary>
        public void Advance() { _advance = true; }

        /// <summary>넘어가기 버튼: 대화 줄을 빠르게 진행한다. 컷신/전투 등 트리거 지점에서는
        /// 멈추고 트리거를 정상 실행한다(다음 컷신·스테이지 이동 등은 그대로 보임).</summary>
        public void SkipAll()
        {
            if (!IsPlaying) return;
            _fastForward = true;
        }

        /// <summary>이름 입력 팝업 확정 시 호출. 저장 후 대사 재개.</summary>
        public void SubmitName(string name)
        {
            SaveSystemManager.I?.SetPlayerName(name);
            _resume = true;
        }

        /// <summary>인터랙티브 컷신 연출 완료 시 호출. 대사 재개.</summary>
        public void ResumeFromInteraction() { _resume = true; }

        IEnumerator Run(string startId)
        {
            IsPlaying = true;
            _fastForward = false;
            _history.Clear();
            string cur = startId;
            string chapter = null;

            while (!string.IsNullOrEmpty(cur) && table.TryGetValue(cur, out var raw))
            {
                // 챕터가 바뀌면 다시보기 히스토리 초기화
                if (chapter != null && raw.chapter != chapter) _history.Clear();
                chapter = raw.chapter;

                var e = raw;
                e.speaker = Substitute(e.speaker);
                e.text    = Substitute(e.text);
                _history.Add(e);

                // 텍스트가 빈 줄 = 연출 전용 줄. 대사창을 띄우지 않고 트리거만 실행한다.
                // (튜토리얼 도입처럼 첫 대사보다 먼저 나와야 하는 컷신을 CSV 순서로 표현하기 위함)
                bool stagingOnly = string.IsNullOrEmpty(e.text);

                if (!stagingOnly)
                {
                    OnLine?.Invoke(e);
                    _advance = false;
                    // 일반: 탭 대기. 빨리감기: 입력 없이 즉시 다음 줄.
                    if (!_fastForward)
                        while (!_advance && !_fastForward) yield return null;
                    else
                        yield return null;
                }

                if (e.trigger != DialogueTrigger.None)
                {
                    _fastForward = false;   // 컷신/이름입력/전투 트리거는 빨리감기 해제하고 정상 실행
                    yield return HandleTrigger(e);
                    if (e.trigger == DialogueTrigger.StartBattle) break;
                }

                cur = e.next;
            }

            IsPlaying = false;
            _fastForward = false;
            SaveSystemManager.I?.MarkDialogueSeen(startId);
            OnEnd?.Invoke();
        }

        IEnumerator HandleTrigger(DialogueEntry e)
        {
            switch (e.trigger)
            {
                case DialogueTrigger.NameInput:
                    _resume = false;
                    OnNameInputRequested?.Invoke();
                    while (!_resume) yield return null;
                    _resume = false;
                    break;
                case DialogueTrigger.StartBattle:
                    OnBattleStartRequested?.Invoke();
                    break;
                case DialogueTrigger.InspectWall:
                case DialogueTrigger.InspectHolyWater:
                case DialogueTrigger.TutorialSummon:
                case DialogueTrigger.TutorialDogLine:
                case DialogueTrigger.TutorialEnemy:
                case DialogueTrigger.TutorialBear:
                    _resume = false;
                    OnInteractionRequested?.Invoke(e.trigger);
                    while (!_resume) yield return null;
                    _resume = false;
                    break;
                case DialogueTrigger.PlayCutscene:
                    Debug.Log($"[Cutscene] HandleTrigger: cutsceneId={e.cutsceneId}, Manager={CutsceneManager.I}");
                    _resume = false;
                    if (CutsceneManager.I != null)
                    {
                        CutsceneManager.I.OnFinished += () => _resume = true;
                        CutsceneManager.I.TryPlay(e.cutsceneId);
                    }
                    else _resume = true;
                    while (!_resume) yield return null;
                    _resume = false;
                    break;
            }
        }

        string Substitute(string text)
        {
            if (string.IsNullOrEmpty(text) || text.IndexOf('{') < 0) return text;
            string name = SaveSystemManager.I != null ? SaveSystemManager.I.PlayerName
                                                      : SaveSystemManager.DefaultPlayerName;
            return text.Replace("{name}", name);
        }
    }
}
