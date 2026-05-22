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

        void Awake()
        {
            if (I != null && I != this) { Destroy(gameObject); return; }
            I = this; DontDestroyOnLoad(gameObject);
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
