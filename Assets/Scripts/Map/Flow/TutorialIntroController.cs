using UnityEngine;
using UnityEngine.UI;
using TOME.Core;
using TOME.Data;
using TOME.Managers;

namespace TOME.Map
{
    /// <summary>첫 실행 시 인트로 튜토리얼(CHAPTER1) 대사를 재생.
    /// 대사 끝(StartBattle 트리거)에서 "싸우자!" UI를 띄우고,
    /// 그 UI를 클릭하면 페이드 전환으로 튜토리얼 전투에 진입한다.</summary>
    public class TutorialIntroController : MonoBehaviour
    {
        [SerializeField] string  startLineId = "c1_01";
        [SerializeField] NodeSO  tutorialNode;     // 전투로 넘길 노드 (예: Node_Hallway)
        [SerializeField] StageSO tutorialStage;    // 전투 스테이지 (예: Stage_1)
        [SerializeField] bool    onlyOnFirstLaunch = true;

        [Header("싸우자 UI")]
        [SerializeField] GameObject fightUi;       // "싸우자!" 클릭 UI 루트 (평소 비활성)
        [SerializeField] Button     fightButton;   // 클릭 시 전투 진입

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
        }

        // "싸우자!" 트리거(c1_34) — 대사 종료 후 싸우자 UI 노출
        void OnBattleStart()
        {
            SaveSystemManager.I?.MarkIntroSeen();
            if (fightUi) fightUi.SetActive(true);
        }

        // 싸우자 UI 클릭 → 페이드 전환(EnterStage 내부에서 SceneFader 사용)
        void OnFightClicked()
        {
            if (fightUi) fightUi.SetActive(false);

            // 튜토리얼은 MapStageGate를 거치지 않으므로 여기서 직접 현재 맵 화면을 캡처해 Stage 배경으로 사용
            var cam = Camera.main;
            if (cam != null && GameManager.I != null)
            {
                var tex = StageBackgroundCapture.Capture(cam);
                GameManager.I.SetPendingBackgroundTexture(tex);
            }

            if (tutorialNode != null && tutorialStage != null)
                GameManager.I?.EnterStage(tutorialNode, tutorialStage);
        }
    }
}
