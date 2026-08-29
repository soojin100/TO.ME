using UnityEngine;
using UnityEngine.UI;
using TOME.Core;
using TOME.Data;
using TOME.Systems;

namespace TOME.Map
{
    /// <summary>스테이지 진입 "싸우자" UI 공용 컨트롤러.
    /// - 튜토리얼: 첫 실행 시 인트로 대사 후 "싸우자" UI 노출 → 클릭 시 튜토리얼 전투 진입.
    /// - 일반: 맵 스테이지 버튼이 Show(node, stage)를 호출 → "싸우자" UI 노출 → 클릭 시 해당 스테이지 진입.
    /// (이전 StageInfoPopup 진입 UI는 사용 안 함)</summary>
    public class TutorialIntroController : MonoBehaviour
    {
        public static TutorialIntroController I { get; private set; }

        [SerializeField] string  startLineId = "c1_01";
        [SerializeField] NodeSO  tutorialNode;     // 튜토리얼 전투 노드
        [SerializeField] StageSO tutorialStage;    // 튜토리얼 전투 스테이지
        [SerializeField] bool    onlyOnFirstLaunch = true;

        [Header("싸우자 UI")]
        [SerializeField] GameObject fightUi;       // "싸우자!" 클릭 UI 루트 (평소 비활성)
        [SerializeField] Button     fightButton;   // 클릭 시 전투 진입

        NodeSO  _node;     // 현재 진입 대상
        StageSO _stage;

        void Awake() { I = this; }

        void Start()
        {
            if (fightUi) fightUi.SetActive(false);
            if (fightButton) fightButton.onClick.AddListener(OnFightClicked);

            if (DialogueManager.I == null) return;
            if (onlyOnFirstLaunch && SaveSystemManager.I != null && SaveSystemManager.I.SeenIntro) return;

            DialogueManager.I.OnBattleStartRequested += OnBattleStart;
            DialogueManager.I.TryPlay(startLineId);
        }

        void OnDestroy()
        {
            if (DialogueManager.I != null)
                DialogueManager.I.OnBattleStartRequested -= OnBattleStart;
            if (fightButton) fightButton.onClick.RemoveListener(OnFightClicked);
            if (I == this) I = null;
        }

        // 튜토리얼 대사 끝(StartBattle 트리거) → 싸우자 UI 노출(튜토리얼 스테이지 대상)
        void OnBattleStart()
        {
            SaveSystemManager.I?.MarkIntroSeen();
            Show(tutorialNode, tutorialStage);
        }

        /// <summary>맵 스테이지 버튼 등에서 호출: 싸우자 UI를 띄우고 진입 대상 스테이지를 지정.</summary>
        public void Show(NodeSO node, StageSO stage)
        {
            _node  = node;
            _stage = stage;
            if (fightUi) fightUi.SetActive(true);
        }

        // 싸우자 클릭 → 현재 맵 화면 캡처(Stage 배경) → 진입
        void OnFightClicked()
        {
            if (fightUi) fightUi.SetActive(false);

            var cam = Camera.main;
            if (cam != null && GameManager.I != null)
            {
                try { GameManager.I.SetPendingBackgroundTexture(StageBackgroundCapture.Capture(cam)); }
                catch (System.Exception e) { Debug.LogWarning($"[Fight] 배경 캡처 실패(무시): {e.Message}"); }
            }

            if (_node != null && _stage != null)
                GameManager.I?.EnterStage(_node, _stage);
        }
    }
}
