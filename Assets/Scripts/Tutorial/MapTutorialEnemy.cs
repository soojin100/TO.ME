using UnityEngine;
using TOME.Core;
using TOME.Data;
using TOME.Systems;
using TOME.Map;

namespace TOME.Tutorial
{
    /// <summary>맵에 배치되는 공격 능력 없는 에너미. 클릭하면 지정 스테이지로 바로 들어간다.
    /// 기획서 p14가 "클릭시 ... 게임맵을 출력"이라 일반 경로의 "싸우자" UI를 거치지 않는다.
    /// 일반 스테이지 진입(StageNodeButton → MapFlowController → 싸우자 UI)은 그대로 둔다.</summary>
    [RequireComponent(typeof(Collider2D))]
    public class MapTutorialEnemy : MonoBehaviour
    {
        [SerializeField] NodeSO  node;
        [SerializeField] StageSO stage;
        [Tooltip("진입 전 현재 맵 화면을 캡처해 Stage 배경으로 넘긴다(일반 진입과 동일한 연출).")]
        [SerializeField] bool captureBackground = true;

        bool _entering;

        /// <summary>스텝이 프리팹을 배치한 뒤 진입 대상을 주입한다.</summary>
        public void Configure(NodeSO targetNode, StageSO targetStage)
        {
            node  = targetNode;
            stage = targetStage;
        }

        void OnMouseDown()
        {
            if (_entering) return;
            // 대사/컷신 중에는 진입시키지 않는다.
            if (DialogueManager.I != null && DialogueManager.I.IsPlaying) return;
            if (node == null || stage == null)
            {
                Debug.LogWarning("[Tutorial] MapTutorialEnemy에 진입 대상 Node/Stage가 없습니다.");
                return;
            }

            _entering = true;

            if (captureBackground)
            {
                var cam = Camera.main;
                if (cam != null && GameManager.I != null)
                {
                    try { GameManager.I.SetPendingBackgroundTexture(StageBackgroundCapture.Capture(cam)); }
                    catch (System.Exception e)
                    {
                        Debug.LogWarning($"[Tutorial] 배경 캡처 실패(무시): {e.Message}");
                    }
                }
            }

            GameManager.I?.EnterStage(node, stage);
        }
    }
}
