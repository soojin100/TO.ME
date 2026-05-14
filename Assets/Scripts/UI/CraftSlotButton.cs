using System;
using UnityEngine;
using UnityEngine.UI;
using TOME.Data;

namespace TOME.UI
{
    /// <summary>조합창 슬롯. 인덱스 고정, 클릭 시 인덱스 콜백.</summary>
    [RequireComponent(typeof(Button))]
    public class CraftSlotButton : MonoBehaviour
    {
        [SerializeField] Image icon;

        Button _btn;
        int _index;
        Action<int> _cb;

        public void Init(int index, Action<int> onClick)
        {
            if (_btn == null) _btn = GetComponent<Button>();
            _index = index;
            _cb = onClick;
            _btn.onClick.AddListener(() => _cb?.Invoke(_index));
        }

        public void Bind(ItemSO item)
        {
            if (icon)
            {
                icon.enabled = item != null && item.icon != null;
                if (item != null && item.icon) icon.sprite = item.icon;
            }
        }
    }
}
