using System;
using UnityEngine;
using UnityEngine.UI;
using TOME.Data;

namespace TOME.UI
{
    /// <summary>인벤토리 바 슬롯. 아이콘 표시 + 클릭 콜백.</summary>
    [RequireComponent(typeof(Button))]
    public class InventorySlotButton : MonoBehaviour
    {
        [SerializeField] Image icon;

        Button _btn;
        ItemSO _item;
        Action<ItemSO> _cb;

        void Awake()
        {
            _btn = GetComponent<Button>();
            _btn.onClick.AddListener(() => { if (_item != null) _cb?.Invoke(_item); });
        }

        public void Bind(ItemSO item, float iconScale, Action<ItemSO> cb)
        {
            _item = item;
            _cb   = cb;
            if (icon)
            {
                icon.enabled = item != null && item.icon != null;
                if (item != null && item.icon) icon.sprite = item.icon;
                icon.transform.localScale = Vector3.one * iconScale;
            }
            _btn.interactable = item != null;
        }

        public void Clear()
        {
            _item = null;
            _cb   = null;
            if (icon) icon.enabled = false;
            _btn.interactable = false;
        }
    }
}
