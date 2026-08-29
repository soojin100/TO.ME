using System.Collections;
using UnityEngine;
using TOME.Core;
using TOME.Data;

namespace TOME.Systems
{
    public enum StageResult { None, Win, Lose }

    public class GameManager : MonoBehaviour
    {
        public static GameManager I { get; private set; }

        public NodeSO   CurrentNode    { get; private set; }
        public StageSO  CurrentStage   { get; private set; }
        public ChapterSO CurrentChapter { get; private set; }
        public StageResult LastStageResult { get; private set; }
        public string PendingPostDialogueId { get; private set; }
        public int       CurrentSectionIndex { get; private set; } = -1;  // -1 = 미지정(중앙 기본)
        public Texture2D PendingBackgroundTexture { get; private set; }

        public void SetPendingSectionIndex(int idx) { CurrentSectionIndex = idx; }

        // 예시용 초반 아이템 지급용 코드 추후 변경시 삭제바람 
        [Header("Tutorial Starter Items")]
        [SerializeField] ItemSO[] starterItems;
        bool _starterGiven;

        public void TryGiveStarterItems()
        {
            if (_starterGiven || starterItems == null || starterItems.Length == 0) return;
            _starterGiven = true;

            if (InventoryManager.I != null)
            {
                InventoryManager.I.Clear();   // 혹시 남은 거 있으면 초기화 후 지급
                foreach (var item in starterItems)
                    if (item != null) InventoryManager.I.Add(item);
            }
        }

        // 게임 재시작 / 챕터 리셋 시 호출
        public void ResetStarterFlag() => _starterGiven = false;
        //여기까지

        public void SetPendingBackgroundTexture(Texture2D tex)
        {
            if (PendingBackgroundTexture != null && PendingBackgroundTexture != tex)
                Destroy(PendingBackgroundTexture);
            PendingBackgroundTexture = tex;
        }

        /// 스테이지 종료(씬 이탈) 시 호출 — 배경으로 쓴 캡처 텍스처를 즉시 해제해 메모리 누수 방지.
        /// 다음 스테이지 진입 시 맵에서 새로 캡처하므로 보관할 필요 없음.
        public void ClearPendingBackgroundTexture()
        {
            if (PendingBackgroundTexture != null) Destroy(PendingBackgroundTexture);
            PendingBackgroundTexture = null;
        }

        void Awake()
        {
            if (I != null && I != this) { Destroy(this); return; }
            I = this;
            if (transform.parent != null) transform.SetParent(null, true);
            DontDestroyOnLoad(gameObject);
        }

        void OnDestroy() { if (I == this) I = null; }

        public void EnterStage(NodeSO node, StageSO stage)
        {
            Time.timeScale = 1f;
            CurrentNode  = node;
            CurrentStage = stage;
            if (stage != null && stage.chapter != null) CurrentChapter = stage.chapter;
            LastStageResult = StageResult.None;
            PendingPostDialogueId = null;
            SceneFader.I.TransitionToScene(SceneKeys.Stage);
        }

        // StageManager.OnFinished에서 호출 — 대사 책임을 맵 씬으로 이전하는 와이어링
        public void RecordStageResult(bool win)
        {
            LastStageResult = win ? StageResult.Win : StageResult.Lose;
            if (win && CurrentStage != null)
                PendingPostDialogueId = CurrentStage.postDialogueId;
        }

        public void ClearPendingPostDialogue() => PendingPostDialogueId = null;

        /// <summary>챕터 보스를 깨면 다음 챕터로 넘긴다. ReturnToMap이 그 챕터의 맵으로 데려간다.
        /// 마지막 챕터이거나 보스가 아니면 아무 일도 하지 않는다.</summary>
        public bool TryAdvanceChapter(NodeSO clearedNode)
        {
            if (clearedNode == null || CurrentChapter == null) return false;
            if (CurrentChapter.finalNode != clearedNode) return false;
            if (CurrentChapter.nextChapter == null) return false;

            CurrentChapter = CurrentChapter.nextChapter;
            CurrentSectionIndex = -1;                     // 새 맵에서는 시작 구역부터
            SaveSystemManager.I?.SetCurrentChapter(CurrentChapter.id);
            Debug.Log($"[GameManager] 챕터 전환 → {CurrentChapter.id} ({CurrentChapter.mapSceneName})");
            return true;
        }

        /// <summary>저장된 진행 챕터를 복원한다. 세이브에 없으면 건드리지 않는다.</summary>
        public void RestoreChapter(ChapterSO[] all)
        {
            string id = SaveSystemManager.I?.Data.currentChapterId;
            if (string.IsNullOrEmpty(id) || all == null) return;
            foreach (ChapterSO c in all)
                if (c != null && c.id == id) { CurrentChapter = c; return; }
        }

        public void ReturnToMap()
        {
            Time.timeScale = 1f;
            // 현재 챕터에 지정된 맵 씬이 있으면 그쪽으로, 없으면 기본 맵
            string scene = (CurrentChapter != null && !string.IsNullOrEmpty(CurrentChapter.mapSceneName))
                           ? CurrentChapter.mapSceneName : SceneKeys.Map;
            SceneFader.I.TransitionToScene(scene);
        }
    }
}
