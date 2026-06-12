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

        [Tooltip("하트가 들어갈 부모(보통 HpContainer). HorizontalLayoutGroup 권장.")]
        [SerializeField] Transform   heartContainer;
        [Tooltip("하트 프리팹. Image(Type=Filled, Fill Method=Horizontal, Origin=Left) + Heart sprite + 빨강.")]
        [SerializeField] Image       heartPrefab;
        [SerializeField] PlayerShell player;
        [SerializeField] TOME.UI.CraftPanelUI craftPanel;
        [SerializeField] RectTransform killBarRoot;
        [SerializeField] GameObject emptyCellPrefab;   
        [SerializeField] GameObject filledCellPrefab;  
        [SerializeField] TMP_Text circleLabel;              
        [SerializeField] Image timerBarFill;        
        [SerializeField] float labelSwapInterval = 1.5f;

        Image[] _hearts;
        float   _lastMax = -1f;

        RectTransform[] _emptyCells;
        GameObject[] _filledCells;


        int _rem, _tot;
        float _currentTime, _maxTime;
        float _labelTimer;
        bool _showEnemy = true;   // true=적 수, false=시간

        void Start()
        {

            // CombatManager가 이미 BeginStage를 호출했을 수도 있으니 최대 시간 바로 세팅
            if (CombatManager.I != null)
            {
                _maxTime = CombatManager.I.TimeLeft;   // BeginStage 직후 HudUI가 Start되면 정확한 값
                CombatManager.I.OnCountChanged += OnCount;
                CombatManager.I.OnTimerChanged += OnTimer;

                // 이미 스테이지 진행 중이면 현재 값으로 즉시 갱신
                OnCount(CombatManager.I.RemainingToKill, CombatManager.I.TotalEnemies);
                OnTimer(CombatManager.I.TimeLeft);
            }
            if (player)
            {
                player.OnHpChanged += OnHp;
                OnHp(player.Hp, player.MaxHp);
            }


        }

        void Update()
        {
            if (circleLabel == null) return;
            _labelTimer += Time.deltaTime;
            if (_labelTimer >= labelSwapInterval)
            {
                _labelTimer = 0f;
                _showEnemy = !_showEnemy;
            }
            circleLabel.text = _showEnemy
                ? $"{_rem}"
                : $"{Mathf.CeilToInt(Mathf.Max(0f, _currentTime))}s";
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

        void OnCount(int rem, int tot)
        {
            _rem = rem;
            _tot = tot;

            if (_emptyCells == null || _emptyCells.Length != tot)
                BuildKillCells(tot);

            int killed = tot - rem;
            for (int i = 0; i < _filledCells.Length; i++)
            {
                // 배열 끝(아래)부터 채움
                bool fill = i >= tot - killed;
                _filledCells[i].SetActive(fill);
            }
        }

        void BuildKillCells(int count)
        {
            foreach (Transform child in killBarRoot) Destroy(child.gameObject);

            _emptyCells = new RectTransform[count];
            _filledCells = new GameObject[count];

            for (int i = 0; i < count; i++)
            {
                // 빈 칸 생성
                var empty = Instantiate(emptyCellPrefab, killBarRoot);
                _emptyCells[i] = empty.GetComponent<RectTransform>();

                // 초록 오브젝트를 빈 칸 자식으로 — 크기 자동으로 맞춰짐
                var filled = Instantiate(filledCellPrefab, empty.transform);
                var filledRt = filled.GetComponent<RectTransform>();

                // 빈 칸에 꽉 차게 앵커 설정
                filledRt.anchorMin = Vector2.zero;
                filledRt.anchorMax = Vector2.one;
                filledRt.offsetMin = Vector2.zero;
                filledRt.offsetMax = Vector2.zero;

                filled.SetActive(false);
                _filledCells[i] = filled;
            }
        }

        void OnTimer(float t)
        {
            _currentTime = t;
            if (_maxTime <= 0f) _maxTime = t;

            Debug.Log($"[Timer] t={t}, maxTime={_maxTime}, fill={Mathf.Clamp01(t / _maxTime)}");

            if (timerBarFill && _maxTime > 0f)
                timerBarFill.fillAmount = Mathf.Clamp01(t / _maxTime);
        }

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
