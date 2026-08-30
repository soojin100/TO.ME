using TMPro;
using UnityEngine;
using UnityEngine.UI;
using TOME.Dialogue;
using TOME.GameFlow;
using TOME.Progression;
using TOME.Save;
namespace TOME.Map
{
    /// <summary>스테이지 진입 정보창(WOOD 팝업) 공용 컨트롤러. (구 원형 "싸우자!" UI를 기획서 개편안으로 교체)
    /// - 스테이지 버튼 클릭 → Show(node, stage): 주변 암전 + 나무판 정보창(제목·클리어 게이지·n/m) 노출.
    /// - 정보창(나무판) 클릭 → 해당 스테이지 데이터로 진입. 정보창 밖(암전) 클릭 → 닫기.
    /// - 정보창이 떠 있는 동안 맵의 강아지는 이동을 멈추고 제자리 애니메이션만 재생.
    /// - 튜토리얼: 첫 실행 시 인트로 대사 후 같은 정보창으로 튜토리얼 전투 진입.</summary>
    public class StageEntryController : MonoBehaviour
    {
        public static StageEntryController I { get; private set; }

        [SerializeField] string  startLineId = "tut1_00";
        [SerializeField] NodeSO  tutorialNode;     // 튜토리얼 전투 노드
        [SerializeField] StageSO tutorialStage;    // 튜토리얼 전투 스테이지
        [SerializeField] bool    onlyOnFirstLaunch = true;

        [Header("스테이지 정보창 (WOOD 팝업)")]
        [Tooltip("정보창 전체 루트(암전 + 나무판). 평소 비활성.")]
        [SerializeField] GameObject     popupRoot;
        [Tooltip("나무판 자체의 버튼. 클릭 시 스테이지 진입.")]
        [SerializeField] Button         panelButton;
        [Tooltip("정보창 밖 전체를 덮는 암전 버튼. 클릭 시 정보창 닫기.")]
        [SerializeField] Button         dimCloseButton;
        [SerializeField] TMP_Text       titleText;
        [Tooltip("클리어 횟수 표기 \"n/m\".")]
        [SerializeField] TMP_Text       countText;
        [Tooltip("게이지 채움 이미지의 RectTransform. anchorMax.x를 비율로 조절한다(둥근 모서리 유지).")]
        [SerializeField] RectTransform  gaugeFill;

        NodeSO  _node;     // 현재 진입 대상
        StageSO _stage;

        void Awake() { I = this; }

        void Start()
        {
            if (popupRoot) popupRoot.SetActive(false);
            if (panelButton)    panelButton.onClick.AddListener(OnPanelClicked);
            if (dimCloseButton) dimCloseButton.onClick.AddListener(Hide);

            if (DialogueManager.I == null) return;
            DialogueManager.I.OnBattleStartRequested += OnBattleStart;

            // 튜토리얼 대사를 튼다. 연출(암전·소환·이름입력·에너미 배치)은 이 대사의
            // trigger 컬럼이 불러내므로, 흐름은 dialogue 시트가 결정한다.
            if (onlyOnFirstLaunch && SaveSystemManager.I != null && SaveSystemManager.I.SeenIntro) return;

            DialogueManager.I.OnEnd += OnTutorialDialogueEnd;
            DialogueManager.I.TryPlay(startLineId);
        }

        // 튜토리얼 대사가 끝나면 다시 보지 않도록 기록한다.
        void OnTutorialDialogueEnd()
        {
            if (DialogueManager.I != null) DialogueManager.I.OnEnd -= OnTutorialDialogueEnd;
            SaveSystemManager.I?.MarkIntroSeen();
        }

        void OnDestroy()
        {
            if (DialogueManager.I != null)
            {
                DialogueManager.I.OnBattleStartRequested -= OnBattleStart;
                DialogueManager.I.OnEnd -= OnTutorialDialogueEnd;
            }
            if (panelButton)    panelButton.onClick.RemoveListener(OnPanelClicked);
            if (dimCloseButton) dimCloseButton.onClick.RemoveListener(Hide);
            if (I == this) I = null;
        }

        // StartBattle 트리거 → 정보창 노출(지정된 튜토리얼 스테이지 대상).
        void OnBattleStart() => Show(tutorialNode, tutorialStage);

        /// <summary>맵 스테이지 버튼 등에서 호출: 정보창을 띄우고 진입 대상 스테이지를 지정.</summary>
        public void Show(NodeSO node, StageSO stage)
        {
            _node  = node;
            _stage = stage;
            RefreshPopup(stage);
            if (popupRoot) popupRoot.SetActive(true);
            SetWanderPaused(true);
        }

        /// <summary>정보창을 닫는다(밖 클릭·진입 공용).</summary>
        public void Hide()
        {
            if (popupRoot) popupRoot.SetActive(false);
            SetWanderPaused(false);
        }

        void RefreshPopup(StageSO stage)
        {
            if (stage == null) return;
            int required = Mathf.Max(1, stage.clearRequirement);
            int count    = SaveSystemManager.I != null
                ? Mathf.Min(SaveSystemManager.I.GetStageClearCount(stage.id), required)
                : 0;

            if (titleText) titleText.text = stage.title;
            if (countText) countText.text = $"{count}/{required}";
            if (gaugeFill)
            {
                // 채움을 anchorMax.x 비율로 표현 — 슬라이스 스프라이트의 둥근 양끝이 유지된다.
                // 0일 때는 찌그러진 조각이 남지 않게 통째로 끈다.
                gaugeFill.gameObject.SetActive(count > 0);
                var a = gaugeFill.anchorMax;
                a.x = (float)count / required;
                gaugeFill.anchorMax = a;
            }
        }

        // 정보창이 떠 있는 동안 강아지는 제자리 애니메이션만(기획서 p4).
        void SetWanderPaused(bool paused)
        {
            var wander = FindAnyObjectByType<CharacterWander>();
            if (wander != null) wander.SetPaused(paused);
        }

        // 나무판 클릭 → 정보창 닫기 → 현재 맵 화면 캡처(Stage 배경) → 진입
        void OnPanelClicked()
        {
            Hide();

            var cam = Camera.main;
            if (cam != null && GameManager.I != null)
            {
                try { GameManager.I.SetPendingBackgroundTexture(StageBackgroundCapture.Capture(cam)); }
                catch (System.Exception e) { Debug.LogWarning($"[StageEntry] 배경 캡처 실패(무시): {e.Message}"); }
            }

            if (_node != null && _stage != null)
                GameManager.I?.EnterStage(_node, _stage);
        }
    }
}
