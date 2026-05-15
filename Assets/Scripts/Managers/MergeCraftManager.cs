using System;
using System.Collections.Generic;
using UnityEngine;
using TOME.Data;
using TOME.Gameplay.Merge;

namespace TOME.Managers
{
    /// 4슬롯 조합창. 결과 확인 → 캐릭터 교체 요청.
    public class MergeCraftManager : MonoBehaviour
    {
        public static MergeCraftManager I { get; private set; }

        public const int SlotCount = 4;
        readonly ItemSO[] slots = new ItemSO[SlotCount];
        readonly List<ItemSO> _previewBuf = new(SlotCount);   // GC 회피
        readonly RecipeMatcher _matcher = new();

        public event Action OnSlotsChanged;
        public event Action<CharacterSO> OnCraftSucceeded;

        void Awake()
        {
            if (I != null && I != this) { Destroy(gameObject); return; }
            I = this;
        }

        void OnDestroy() { if (I == this) I = null; }

        /// 씬별 조합 규칙 주입. StageManager / MapStageGate가 호출.
        public void SetRecipes(IEnumerable<RecipeSO> recipes) => _matcher.Init(recipes);

        public ItemSO GetSlot(int i) => (i >= 0 && i < SlotCount) ? slots[i] : null;

        public bool PlaceFromInventory(int slotIdx, ItemSO item)
        {
            if (slotIdx < 0 || slotIdx >= SlotCount) return false;
            if (slots[slotIdx] != null) return false;
            slots[slotIdx] = item;
            InventoryManager.I?.Remove(item);
            OnSlotsChanged?.Invoke();
            return true;
        }

        public void ReturnSlotToInventory(int slotIdx)
        {
            if (slotIdx < 0 || slotIdx >= SlotCount) return;
            var it = slots[slotIdx];
            if (!it) return;
            slots[slotIdx] = null;
            InventoryManager.I?.Add(it);
            OnSlotsChanged?.Invoke();
        }

        public CharacterSO Preview()
        {
            _previewBuf.Clear();
            for (int i = 0; i < SlotCount; i++) if (slots[i]) _previewBuf.Add(slots[i]);
            var recipe = _matcher.Match(_previewBuf);
            return recipe ? recipe.result : null;
        }

        /// 결과창 클릭 시 호출
        public bool Craft()
        {
            var ch = Preview();
            if (!ch) return false;
            for (int i = 0; i < SlotCount; i++) slots[i] = null;
            OnSlotsChanged?.Invoke();
            OnCraftSucceeded?.Invoke(ch);
            return true;
        }
    }
}
