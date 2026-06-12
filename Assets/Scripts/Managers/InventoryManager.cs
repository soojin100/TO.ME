using System;
using System.Collections.Generic;
using UnityEngine;
using TOME.Data;

namespace TOME.Managers
{
    /// 맵↔스테이지 공유 인벤토리. 데이터는 정적으로 세션 동안 유지된다.
    public class InventoryManager : MonoBehaviour
    {
        public static InventoryManager I { get; private set; }

        static readonly List<ItemSO> items = new(16);   // 정적: 씬 전환에도 유지(맵↔스테이지 공유)
        public IReadOnlyList<ItemSO> Items => items;

        public event Action OnChanged;

        void Awake()
        {
            if (I != null && I != this) { Destroy(this); return; }
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

            // CSV 대화 없이 바로 컷신 재생
            if (!string.IsNullOrEmpty(item.onFirstPickupDialogueId))
            {
                CutsceneManager.I?.TryPlay(item.onFirstPickupDialogueId);
            }

            return true;
        }

        void OnDialogueEnd()
        {
            CombatManager.I?.Resume();
            if (DialogueManager.I != null)
                DialogueManager.I.OnEnd -= OnDialogueEnd;
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
