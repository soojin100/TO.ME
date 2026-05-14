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

        public NodeSO  CurrentNode  { get; private set; }
        public StageSO CurrentStage { get; private set; }
        public StageResult LastStageResult { get; private set; }
        public string PendingPostDialogueId { get; private set; }

        void Awake()
        {
            if (I != null && I != this) { Destroy(gameObject); return; }
            I = this; DontDestroyOnLoad(gameObject);
        }

        public void EnterStage(NodeSO node, StageSO stage)
        {
            Time.timeScale = 1f;
            CurrentNode  = node;
            CurrentStage = stage;
            LastStageResult = StageResult.None;
            PendingPostDialogueId = null;
            StartCoroutine(SceneLoader.LoadAsync(SceneKeys.Stage));
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
            StartCoroutine(SceneLoader.LoadAsync(SceneKeys.Map));
        }
    }
}
