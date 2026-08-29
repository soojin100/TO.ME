using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TOME.Data;
using TOME.Systems;

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

            // 비활성화. 
            if (resultIcon) resultIcon.enabled = false;
            if (resultLabel) resultLabel.text = "";
            if (resultButton) resultButton.interactable = false;
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

            // 조합 결과 미리보기 가져오기
            var result = MergeCraftManager.I.Preview();
            bool canCraft = result != null;

            if (canCraft)
            {
                // 재료가 올바르게 올라와서 조합 결과(적토견, 폴백 등)가 존재할 때
                if (resultIcon)
                {
                    resultIcon.sprite = result.icon;       // 캐릭터의 아이콘 연결 [cite: 25, 93]
                    resultIcon.enabled = result.icon != null; // 아이콘 이미지가 있으면 켜기 [cite: 25]
                }
                if (resultLabel)
                {
                    resultLabel.text = result.displayName; // 캐릭터 이름 출력 (예: 적토견) [cite: 53]
                }
            }
            else
            {
                // 슬롯이 비어있거나 조합할 수 없는 상태일 때 완전히 비우기
                if (resultIcon) resultIcon.enabled = false;
                if (resultLabel) resultLabel.text = "";
            }

            if (resultButton) resultButton.interactable = canCraft;
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
