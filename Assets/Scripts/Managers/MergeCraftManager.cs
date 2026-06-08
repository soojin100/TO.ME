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

        [SerializeField] CharacterSO fallbackResult;   // 레시피 미일치 시 결과(똥 캐릭터)

        readonly ItemSO[] slots = new ItemSO[SlotCount];
        readonly List<ItemSO> _previewBuf = new(SlotCount);   // GC 회피
        readonly RecipeMatcher _matcher = new();

        public event Action OnSlotsChanged;
        public event Action<CharacterSO> OnCraftSucceeded;

        /// 직전 Preview() 결과가 레시피 미일치 폴백(똥강아지)인지 여부.
        /// 실제 레시피 결과는 결과창에서 숨기되, 폴백(똥강아지)은 보여주기 위해 사용.
        public bool LastPreviewWasFallback { get; private set; }

        void Awake()
        {
            if (I != null && I != this) { Destroy(this); return; }
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
            if (recipe) { LastPreviewWasFallback = false; return recipe.result; }
            // 레시피가 없어도 아이템이 1개 이상 있으면 폴백(똥강아지) 결과
            LastPreviewWasFallback = _previewBuf.Count > 0;
            return _previewBuf.Count > 0 ? fallbackResult : null;
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
