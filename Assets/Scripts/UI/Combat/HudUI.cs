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
        [Tooltip("하단 인벤토리 진입 버튼 루트(선택). StageSO.allowInventory가 false면 숨긴다.")]
        [SerializeField] GameObject inventoryButtonRoot;
        [Tooltip("라운드 표시(선택). 라운드가 2개 이상인 스테이지에서만 보인다.")]
        [SerializeField] TMP_Text roundLabel;
        [Tooltip("라운드 표시 형식. {0}=현재 라운드, {1}=총 라운드 수, {2}=라운드 이름.")]
        [SerializeField] string roundFormat = "{2}  {0}/{1}";

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
                CombatManager.I.OnRoundChanged += OnRound;
                CombatManager.I.OnTimerChanged += OnTimer;

                // 이미 스테이지 진행 중이면 현재 값으로 즉시 갱신
                OnCount(CombatManager.I.RemainingToKill, CombatManager.I.TotalEnemies);
                OnTimer(CombatManager.I.TimeLeft);
                OnRound(CombatManager.I.RoundNumber, CombatManager.I.RoundCount, CombatManager.I.RoundLabel);
            }
            if (player)
            {
                player.OnHpChanged += OnHp;
                OnHp(player.Hp, player.MaxHp);
            }

            // 튜토리얼 전투 등 조합이 잠긴 스테이지에서는 진입 버튼 자체를 숨긴다(기획서 p15).
            if (inventoryButtonRoot && !InventoryAllowed())
                inventoryButtonRoot.SetActive(false);


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
                CombatManager.I.OnRoundChanged -= OnRound;
            }
            if (player) player.OnHpChanged -= OnHp;
        }

        // 라운드가 하나뿐인 스테이지에서는 굳이 표시하지 않는다(화면만 복잡해진다).
        void OnRound(int number, int count, string label)
        {
            if (roundLabel == null) return;
            bool show = count > 1 && number > 0;
            roundLabel.gameObject.SetActive(show);
            if (show) roundLabel.text = string.Format(roundFormat, number, count, label);
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
            // 버튼을 숨기지 못한 경로(직접 호출 등)도 여기서 차단한다.
            if (!InventoryAllowed()) return;
            CombatManager.I?.Pause();
            if (craftPanel) craftPanel.Open();
        }

        // 현재 스테이지가 조합창 사용을 허용하는지. 스테이지 정보가 없으면 허용(기존 동작 유지).
        static bool InventoryAllowed()
        {
            var stage = GameManager.I != null ? GameManager.I.CurrentStage : null;
            return stage == null || stage.allowInventory;
        }

        /// CraftPanelUI가 닫힐 때 호출.
        public void OnClickCloseCraft()
        {
            CombatManager.I?.Resume();
        }
    }
}
