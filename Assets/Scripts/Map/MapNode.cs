using UnityEngine;
using UnityEngine.UI;
using TOME.Data;
using TOME.Managers;

namespace TOME.Map
{
    /// <summary>UGUI Button 기반. Map 캔버스에 배치. SpriteRenderer 의존 제거.</summary>
    [RequireComponent(typeof(Button))]
    public class MapNode : MonoBehaviour
    {
        [SerializeField] NodeSO def;
        [SerializeField] Image  icon;        // 노드 아이콘
        [SerializeField] Color  lockedColor   = new(0.4f, 0.4f, 0.4f, 1f);
        [SerializeField] Color  unlockedColor = Color.white;

        Button _btn;
        bool _lastUnlocked;

        void Awake()
        {
            _btn = GetComponent<Button>();
            _btn.onClick.AddListener(OnClick);
            if (def && icon && def.iconOnMap) icon.sprite = def.iconOnMap;
        }

        void Start() { Refresh(true); }
        void OnEnable() { Refresh(true); }

        void Refresh(bool force)
        {
            if (!def) return;
            bool u = MapManager.I != null && MapManager.I.IsUnlocked(def);
            if (!force && u == _lastUnlocked) return;
            _lastUnlocked = u;
            _btn.interactable = u;
            if (icon) icon.color = u ? unlockedColor : lockedColor;
        }

        /// <summary>해금 상태 변경 시 MapManager가 브로드캐스트하면 호출.</summary>
        public void NotifyUnlockChanged() => Refresh(false);

        void OnClick()
        {
            if (!def || MapManager.I == null || !MapManager.I.IsUnlocked(def)) return;
            if (def.stages == null || def.stages.Length == 0) return;
            GameManager.I?.EnterStage(def, def.stages[0]);
        }
    }
}
