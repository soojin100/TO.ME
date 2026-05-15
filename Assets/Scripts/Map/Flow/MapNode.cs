using UnityEngine;
using UnityEngine.UI;
using TOME.Data;
using TOME.Managers;

namespace TOME.Map
{
    /// <summary>UGUI Button 기반. 클릭 시 MapFlowController로 위임.</summary>
    [RequireComponent(typeof(Button))]
    public class MapNode : MonoBehaviour
    {
        [SerializeField] NodeSO  def;
        [SerializeField] Image   icon;
        [SerializeField] MapFlowController flow;
        [SerializeField] Color   lockedColor   = new(0.4f, 0.4f, 0.4f, 1f);
        [SerializeField] Color   unlockedColor = Color.white;

        Button _btn;

        void Awake()
        {
            _btn = GetComponent<Button>();
            _btn.onClick.AddListener(OnClick);
            if (def && icon && def.iconOnMap) icon.sprite = def.iconOnMap;
        }

        void Start()    { Refresh(); }
        void OnEnable() { Refresh(); }

        void Refresh()
        {
            if (!def) return;
            bool u = MapManager.I != null && MapManager.I.IsUnlocked(def);
            _btn.interactable = u;
            if (icon) icon.color = u ? unlockedColor : lockedColor;
        }

        void OnClick()
        {
            if (!def || MapManager.I == null || !MapManager.I.IsUnlocked(def)) return;
            if (flow) flow.OnNodeSelected(def);
        }
    }
}
