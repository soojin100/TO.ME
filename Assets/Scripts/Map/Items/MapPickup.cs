using UnityEngine;
using TOME.Core;
using TOME.Data;
using TOME.Managers;
using Interactables;

namespace TOME.Map
{
    /// <summary>맵 줍기 오브젝트(SpriteRenderer + Collider2D). 첫 클릭은 선택(SpriteHighlight),
    /// 두 번째 클릭에 인벤토리에 들어간다. 다른 Pickup을 클릭하면 이전 선택은 해제.
    /// 줍기 상태는 SaveSystem에 영속.</summary>
    [RequireComponent(typeof(SpriteRenderer))]
    [RequireComponent(typeof(Collider2D))]
    public class MapPickup : MonoBehaviour
    {
        static MapPickup _currentSelected;

        [SerializeField] string pickupId;   // 씬 내 고유 ID — 저장 키
        [SerializeField] ItemSO item;

        SpriteHighlight _highlight;
        bool _selected;

        void Awake()
        {
            _highlight = GetComponent<SpriteHighlight>();
        }

        void Start()
        {
            if (!string.IsNullOrEmpty(pickupId)
                && SaveSystemManager.I != null
                && SaveSystemManager.I.IsPickupCollected(pickupId))
            {
                gameObject.SetActive(false);
            }
        }

        void OnDisable()
        {
            if (_currentSelected == this) _currentSelected = null;
        }

        void OnMouseDown()
        {
            if (_selected) { Pick(); return; }

            if (_currentSelected != null && _currentSelected != this)
                _currentSelected.SetSelected(false);

            SetSelected(true);
        }

        void SetSelected(bool on)
        {
            _selected = on;
            if (_highlight != null)
            {
                if (on) _highlight.ShowHighlight();
                else _highlight.HideHighlight();
            }
            _currentSelected = on ? this : (_currentSelected == this ? null : _currentSelected);
        }

        void Pick()
        {
            if (item != null) InventoryManager.I?.Add(item);
            if (!string.IsNullOrEmpty(pickupId))
                SaveSystemManager.I?.MarkPickupCollected(pickupId);
            if (_currentSelected == this) _currentSelected = null;
            gameObject.SetActive(false);
        }
    }
}
