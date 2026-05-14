using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TOME.Managers;
using TOME.Gameplay.Player;

namespace TOME.UI.Combat
{
    /// 상단 적남은수·타이머·HP, 하단 인벤토리 진입 버튼.
    public class HudUI : MonoBehaviour
    {
        [SerializeField] TMP_Text    countLabel;
        [SerializeField] TMP_Text    timerLabel;
        [SerializeField] Slider      hpBar;
        [SerializeField] PlayerShell player;
        [SerializeField] TOME.UI.CraftPanelUI craftPanel;

        void Start()
        {
            if (CombatManager.I != null)
            {
                CombatManager.I.OnCountChanged += OnCount;
                CombatManager.I.OnTimerChanged += OnTimer;
            }
            if (player) player.OnHpChanged += OnHp;
        }

        void OnDestroy()
        {
            if (CombatManager.I != null)
            {
                CombatManager.I.OnCountChanged -= OnCount;
                CombatManager.I.OnTimerChanged -= OnTimer;
            }
            if (player) player.OnHpChanged -= OnHp;
        }

        void OnCount(int rem, int tot) { if (countLabel) countLabel.text = $"남은적 {tot - rem}/{tot}"; }
        void OnTimer(float t)          { if (timerLabel) timerLabel.text = $"{Mathf.CeilToInt(Mathf.Max(0f, t))}s"; }
        void OnHp(int hp, int max)     { if (hpBar) hpBar.value = (float)hp / Mathf.Max(1, max); }

        /// 하단 인벤토리 버튼 OnClick에 연결.
        public void OnClickInventory()
        {
            CombatManager.I?.Pause();
            if (craftPanel) craftPanel.Open();
        }

        /// CraftPanelUI가 닫힐 때 호출.
        public void OnClickCloseCraft()
        {
            CombatManager.I?.Resume();
        }
    }
}
