using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TOME.Data;
using TOME.Managers;

namespace TOME.UI
{
    /// <summary>InventoryManager 바인딩. 가로 페이징, 아이콘 30% 축소 표시.</summary>
    public class InventoryBarUI : MonoBehaviour
    {
        [SerializeField] Transform  slotContainer;
        [SerializeField] GameObject slotButtonPrefab;   // InventorySlotButton
        [SerializeField] Button     prevButton;
        [SerializeField] Button     nextButton;
        [SerializeField] int        visibleCount = 4;
        [SerializeField] float      iconScale    = 0.7f;

        public event Action<ItemSO> OnItemClicked;

        readonly List<InventorySlotButton> _buttons = new();
        int _page;
        bool _wired;

        void OnEnable()
        {
            if (InventoryManager.I != null) InventoryManager.I.OnChanged += Refresh;
            if (!_wired)
            {
                if (prevButton) prevButton.onClick.AddListener(PrevPage);
                if (nextButton) nextButton.onClick.AddListener(NextPage);
                _wired = true;
            }
            _page = 0;
            Refresh();
        }

        void OnDisable()
        {
            if (InventoryManager.I != null) InventoryManager.I.OnChanged -= Refresh;
        }

        void EnsureButtons()
        {
            // 씬에 미리 배치된 자식 슬롯이 있으면 먼저 흡수 (에디터에서 4칸이 보이도록)
            if (_buttons.Count == 0 && slotContainer != null)
            {
                for (int i = 0; i < slotContainer.childCount && _buttons.Count < visibleCount; i++)
                {
                    if (slotContainer.GetChild(i).TryGetComponent<InventorySlotButton>(out var existing))
                        _buttons.Add(existing);
                }
            }
            // 부족하면 프리팹으로 채움
            while (_buttons.Count < visibleCount && slotButtonPrefab && slotContainer)
            {
                var go = Instantiate(slotButtonPrefab, slotContainer);
                if (go.TryGetComponent<InventorySlotButton>(out var sb))
                    _buttons.Add(sb);
                else { Destroy(go); break; }
            }
        }

        public void Refresh()
        {
            EnsureButtons();
            var items = InventoryManager.I != null ? InventoryManager.I.Items : null;
            int count = items?.Count ?? 0;
            int maxPage = Mathf.Max(0, (count - 1) / Mathf.Max(1, visibleCount));
            _page = Mathf.Clamp(_page, 0, maxPage);
            int start = _page * visibleCount;

            for (int i = 0; i < _buttons.Count; i++)
            {
                int idx = start + i;
                if (items != null && idx < count)
                    _buttons[i].Bind(items[idx], iconScale, OnSlotClicked);
                else
                    _buttons[i].Clear();
            }
            // 아이템 수가 visibleCount 이하이면 화살표 자체를 숨김 (회색 처리 X)
            bool needsPaging = count > visibleCount;
            if (prevButton) prevButton.gameObject.SetActive(needsPaging && _page > 0);
            if (nextButton) nextButton.gameObject.SetActive(needsPaging && _page < maxPage);
        }

        void OnSlotClicked(ItemSO item) => OnItemClicked?.Invoke(item);
        void PrevPage() { _page = Mathf.Max(0, _page - 1); Refresh(); }
        void NextPage() { _page++; Refresh(); }
    }
}
