using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TOME.Managers;
using TOME.Gameplay.Player;

namespace TOME.UI.Combat
{
    /// 상단 적남은수·타이머·HP, 하단 인벤토리 진입 버튼.
    /// HP는 heartContainer 자식으로 heartPrefab을 MaxHp만큼 동적 생성. MaxHp가 늘면 자동 추가.
    public class HudUI : MonoBehaviour
    {
        [SerializeField] TMP_Text    countLabel;
        [SerializeField] TMP_Text    timerLabel;
        [Tooltip("하트가 들어갈 부모(보통 HpContainer). HorizontalLayoutGroup 권장.")]
        [SerializeField] Transform   heartContainer;
        [Tooltip("하트 프리팹. Image(Type=Filled, Fill Method=Horizontal, Origin=Left) + Heart sprite + 빨강.")]
        [SerializeField] Image       heartPrefab;
        [SerializeField] PlayerShell player;
        [SerializeField] TOME.UI.CraftPanelUI craftPanel;

        Image[] _hearts;
        float   _lastMax = -1f;

        void Start()
        {
            if (CombatManager.I != null)
            {
                CombatManager.I.OnCountChanged += OnCount;
                CombatManager.I.OnTimerChanged += OnTimer;
            }
            if (player)
            {
                player.OnHpChanged += OnHp;
                OnHp(player.Hp, player.MaxHp);
            }
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

        void OnCount(int rem, int tot) { if (countLabel) countLabel.text = $"남은적 {rem}/{tot}"; }
        void OnTimer(float t)          { if (timerLabel) timerLabel.text = $"{Mathf.CeilToInt(Mathf.Max(0f, t))}s"; }

        void OnHp(float hp, float max)
        {
            if (heartContainer == null || heartPrefab == null) return;

            // MaxHp가 바뀌면 자식 재구성 (스토리 진행으로 늘어날 수 있음)
            if (!Mathf.Approximately(max, _lastMax))
            {
                _lastMax = max;
                RebuildHearts(Mathf.Max(0, Mathf.CeilToInt(max)));
            }

            if (_hearts == null) return;
            float remaining = Mathf.Max(0f, hp);
            for (int i = 0; i < _hearts.Length; i++)
            {
                if (!_hearts[i]) continue;
                _hearts[i].fillAmount = Mathf.Clamp01(remaining); // 0/0.5/1
                remaining -= 1f;
            }
        }

        void RebuildHearts(int count)
        {
            // 기존 자식 제거
            for (int i = heartContainer.childCount - 1; i >= 0; i--)
                Destroy(heartContainer.GetChild(i).gameObject);

            var list = new Image[count];
            for (int i = 0; i < count; i++)
            {
                var img = Instantiate(heartPrefab, heartContainer);
                img.gameObject.SetActive(true);
                list[i] = img;
            }
            _hearts = list;
        }

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
