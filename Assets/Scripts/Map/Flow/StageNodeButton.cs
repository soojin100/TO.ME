using UnityEngine;
using UnityEngine.UI;
using TOME.Data;

namespace TOME.Map
{
    /// <summary>맵의 스테이지 버튼. 클릭 시 MapFlowController.OnNodeSelected(node)로 위임한다
    /// (→ 사전 대사 → StageInfoPopup → 진입). flow는 런타임에 자동 탐색하므로 node만 지정하면 된다.</summary>
    [RequireComponent(typeof(Button))]
    public class StageNodeButton : MonoBehaviour
    {
        [SerializeField] NodeSO node;

        void Awake()
        {
            GetComponent<Button>().onClick.AddListener(OnClick);
        }

        void OnClick()
        {
            if (node == null) return;
            var flow = FindAnyObjectByType<MapFlowController>();
            if (flow != null) flow.OnNodeSelected(node);
        }
    }
}
