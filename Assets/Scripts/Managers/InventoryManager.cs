using System;
using System.Collections.Generic;
using UnityEngine;
using TOME.Data;

namespace TOME.Managers
{
    /// 스테이지 단위 인벤토리. 스테이지 시작 시 Clear, 종료 시 폐기.
    public class InventoryManager : MonoBehaviour
    {
        public static InventoryManager I { get; private set; }

        readonly List<ItemSO> items = new(16);     // 인벤토리 바
        public IReadOnlyList<ItemSO> Items => items;

        public event Action OnChanged;

        void Awake()
        {
            if (I != null && I != this) { Destroy(gameObject); return; }
            I = this;
        }

        void OnDestroy() { if (I == this) I = null; }

        public void Clear()
        {
            items.Clear();
            OnChanged?.Invoke();
        }

        public bool Add(ItemSO item)
        {
            if (!item) return false;
            items.Add(item);
            OnChanged?.Invoke();
            return true;
        }

        public bool Remove(ItemSO item)
        {
            int idx = items.IndexOf(item);
            if (idx < 0) return false;
            items.RemoveAt(idx);
            OnChanged?.Invoke();
            return true;
        }
    }
}
