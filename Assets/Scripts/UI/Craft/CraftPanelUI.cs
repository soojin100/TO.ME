using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TOME.Data;
using TOME.Managers;
using TOME.UI.Combat;

namespace TOME.UI
{
    /// <summary>4슬롯 조합창 + 결과창. MergeCraftManager 바인딩.</summary>
    public class CraftPanelUI : MonoBehaviour
    {
        [SerializeField] GameObject       root;
        [SerializeField] InventoryBarUI   inventoryBar;
        [SerializeField] CraftSlotButton[] craftSlots;   // 4개
        [SerializeField] Button           resultButton;
        [SerializeField] Image            resultIcon;
        [SerializeField] TMP_Text         resultLabel;
        [SerializeField] Button           closeButton;
        [SerializeField] HudUI            hud;

        void Awake()
        {
            // root 초기 비활성은 씬 상태에 맡긴다. Awake에서 SetActive(false) 호출 시
            // root가 자기 자신/조상이면 Open() 직후 Awake가 다시 끄는 회로 발생.
            for (int i = 0; i < craftSlots.Length; i++)
            {
                int idx = i;
                craftSlots[i].Init(idx, OnCraftSlotClicked);
            }
            if (resultButton) resultButton.onClick.AddListener(OnResultClicked);
            if (closeButton)  closeButton.onClick.AddListener(OnClose);

            // 인벤토리 바는 항상 활성 상태이므로 Awake에서 한 번만 구독.
            // 패널이 닫혀 있어도 슬롯 클릭으로 자동 열림.
            if (inventoryBar) inventoryBar.OnItemClicked += OnInventoryItemClicked;
        }

        void OnEnable()
        {
            if (MergeCraftManager.I != null) MergeCraftManager.I.OnSlotsChanged += RefreshSlots;
        }

        void OnDisable()
        {
            if (MergeCraftManager.I != null) MergeCraftManager.I.OnSlotsChanged -= RefreshSlots;
        }

        void OnDestroy()
        {
            if (inventoryBar) inventoryBar.OnItemClicked -= OnInventoryItemClicked;
        }

        /// <summary>HudUI가 인벤토리 버튼 클릭 시 호출.</summary>
        public void Open()
        {
            if (root) root.SetActive(true);
            RefreshSlots();
            if (inventoryBar) inventoryBar.Refresh();
        }

        void OnClose()
        {
            if (MergeCraftManager.I != null)
                for (int i = 0; i < MergeCraftManager.SlotCount; i++)
                    MergeCraftManager.I.ReturnSlotToInventory(i);
            if (root) root.SetActive(false);
            if (hud) hud.OnClickCloseCraft();
        }

        void OnInventoryItemClicked(ItemSO item)
        {
            if (MergeCraftManager.I == null) return;

            // 패널이 닫혀 있으면 먼저 열기
            if (root && !root.activeSelf) Open();

            for (int i = 0; i < MergeCraftManager.SlotCount; i++)
            {
                if (MergeCraftManager.I.GetSlot(i) == null)
                {
                    MergeCraftManager.I.PlaceFromInventory(i, item);
                    break;
                }
            }
        }

        void OnCraftSlotClicked(int idx)
        {
            MergeCraftManager.I?.ReturnSlotToInventory(idx);
        }

        void RefreshSlots()
        {
            if (MergeCraftManager.I == null) return;
            for (int i = 0; i < craftSlots.Length; i++)
                craftSlots[i].Bind(MergeCraftManager.I.GetSlot(i));

            var preview = MergeCraftManager.I.Preview();
            if (resultIcon)
            {
                resultIcon.enabled = preview != null && preview.icon != null;
                if (preview != null && preview.icon) resultIcon.sprite = preview.icon;
            }
            if (resultLabel)  resultLabel.text = preview != null ? preview.displayName : "";
            if (resultButton) resultButton.interactable = preview != null;
        }

        void OnResultClicked()
        {
            if (MergeCraftManager.I != null && MergeCraftManager.I.Craft())
            {
                if (root) root.SetActive(false);
            }
        }
    }
}
