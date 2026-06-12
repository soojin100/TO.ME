using System.Collections;
using UnityEngine;
using TOME.Core;
using TOME.Data;

namespace TOME.Managers
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
