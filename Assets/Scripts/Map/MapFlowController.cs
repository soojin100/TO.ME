using UnityEngine;
using TOME.Data;
using TOME.Managers;
using TOME.UI;

namespace TOME.Map
{
    /// <summary>노드 선택 → 사전 대사 → 팝업. 맵 진입 시 사후 대사 재생.</summary>
    public class MapFlowController : MonoBehaviour
    {
        [SerializeField] StageInfoPopupUI stagePopup;

        NodeSO  _pendingNode;
        StageSO _pendingStage;
        bool    _waitingForPreDialogue;

        void OnEnable()
        {
            if (DialogueManager.I != null) DialogueManager.I.OnEnd += OnDialogueEnd;
        }

        void OnDisable()
        {
            if (DialogueManager.I != null) DialogueManager.I.OnEnd -= OnDialogueEnd;
        }

        void Start()
        {
            // 맵 복귀 시 사후 대사
            if (GameManager.I != null && !string.IsNullOrEmpty(GameManager.I.PendingPostDialogueId))
            {
                string id = GameManager.I.PendingPostDialogueId;
                GameManager.I.ClearPendingPostDialogue();
                DialogueManager.I?.TryPlay(id);
            }
        }

        /// <summary>MapNode가 클릭 시 호출.</summary>
        public void OnNodeSelected(NodeSO node)
        {
            if (DialogueManager.I != null && DialogueManager.I.IsPlaying) return;
            if (node == null || node.stages == null || node.stages.Length == 0) return;
            _pendingNode  = node;
            _pendingStage = node.stages[0];

            bool started = DialogueManager.I != null
                        && DialogueManager.I.TryPlay(_pendingStage.preDialogueId);
            if (started) _waitingForPreDialogue = true;
            else         ShowPopup();
        }

        void OnDialogueEnd()
        {
            if (!_waitingForPreDialogue) return;
            _waitingForPreDialogue = false;
            ShowPopup();
        }

        void ShowPopup()
        {
            if (stagePopup && _pendingNode && _pendingStage)
                stagePopup.Show(_pendingNode, _pendingStage);
        }
    }
}
