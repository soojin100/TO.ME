using UnityEngine;
using UnityEngine.UI;
using TOME.Progression;
namespace TOME.Map
{
    /// <summary>맵의 스테이지 버튼. 클릭 시 MapFlowController.OnNodeSelected(node)로 위임한다
    /// (→ 사전 대사 → StageInfoPopup → 진입). flow는 런타임에 자동 탐색하므로 node만 지정하면 된다.
    /// 아직 해금되지 않은 노드는 눌리지 않는다 — MapManager의 해금 집합을 그대로 따른다.</summary>
    [RequireComponent(typeof(Button))]
    public class StageNodeButton : MonoBehaviour
    {
        [SerializeField] NodeSO node;

        Button _button;

        public NodeSO Node => node;

        void Awake()
        {
            _button = GetComponent<Button>();
            _button.onClick.AddListener(OnClick);
        }

        void OnEnable() => RefreshLock();

        /// <summary>해금 여부를 버튼 활성 상태에 반영한다. 맵에 들어올 때마다 호출된다.</summary>
        public void RefreshLock()
        {
            if (_button == null || node == null) return;
            // MapManager가 아직 없으면(부트 순서) 막지 않는다 — 다음 OnEnable에서 다시 맞춰진다.
            _button.interactable = MapProgressionManager.I == null || MapProgressionManager.I.IsUnlocked(node);
        }

        void OnClick()
        {
            if (node == null) return;
            if (MapProgressionManager.I != null && !MapProgressionManager.I.IsUnlocked(node)) return;

            var flow = FindAnyObjectByType<MapFlowController>();
            if (flow != null) flow.OnNodeSelected(node);
        }
    }
}
