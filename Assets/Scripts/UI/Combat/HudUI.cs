using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TOME.Managers;
using TOME.Gameplay.Player;

namespace TOME.UI.Combat
{
    /// 상단 적남은수·타이머·HP, 하단 인벤토리바 진입 버튼.
    public class HudUI : MonoBehaviour
    {
        [SerializeField] TMP_Text countLabel;
        [SerializeField] TMP_Text timerLabel;
        [SerializeField] Slider   hpBar;
        [SerializeField] PlayerShell player;
        [SerializeField] GameObject craftPanel;

        void Start()
        {
            if (CombatManager.I != null)
            {
                CombatManager.I.OnCountChanged += (rem, tot) => countLabel.text = $"남은적 {tot - rem}/{tot}";
                CombatManager.I.OnTimerChanged += t => timerLabel.text = $"{Mathf.CeilToInt(t)}s";
            }
            if (player)
            {
                player.OnHpChanged += (hp, max) => hpBar.value = (float)hp / Mathf.Max(1, max);
            }
        }

        public void OnClickInventory()
        {
            CombatManager.I?.Pause();
            if (craftPanel) craftPanel.SetActive(true);
        }

        public void OnClickCloseCraft()
        {
            if (craftPanel) craftPanel.SetActive(false);
            CombatManager.I?.Resume();
        }
    }
}
