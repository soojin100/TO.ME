using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TOME.Core;
using TOME.Data;
using TOME.Managers;

namespace TOME.Map
{
    /// <summary>맵에서 조합을 성공하면 스테이지 버튼을 잠금 해제한다. 해제 상태는 저장에 영속.</summary>
    public class MapStageGate : MonoBehaviour
    {
        [SerializeField] List<RecipeSO> recipes;       // 맵에서 가능한 조합 목록
        [SerializeField] CharacterSO    unlockResult;  // 이 결과물을 조합하면 스테이지 해제
        [SerializeField] Button         stageButton;   // 해제 대상 스테이지 버튼
        [Tooltip("이 노드가 속한 ScrollSections 인덱스. Stage 배경 위치 결정에 사용. Section_0_FarLeft=0, ..., Section_6_FarRight=6, Section_3_Center=3.")]
        [SerializeField] int            sectionIndex = 3;
        [Tooltip("설정하면 이 트랜스폼의 월드 X로 구역 인덱스를 역산한다(기기 종횡비 대응). 비우면 위 sectionIndex를 그대로 쓴다.")]
        [SerializeField] Transform      sectionAnchor;

        /// <summary>실제로 사용할 구역 인덱스. sectionAnchor가 있으면 월드 위치에서 역산한다.</summary>
        int SectionIndex =>
            sectionAnchor != null && ScreenNavigator.Instance != null
                ? ScreenNavigator.Instance.SectionIndexAtWorldX(sectionAnchor.position.x)
                : sectionIndex;

        void Start()
        {
            MergeCraftManager.I?.SetRecipes(recipes);

            bool alreadyUnlocked = unlockResult != null
                && SaveSystemManager.I != null
                && SaveSystemManager.I.IsCharUnlocked(unlockResult.id);

            if (stageButton)
            {
                stageButton.interactable = alreadyUnlocked;
                stageButton.onClick.AddListener(OnStageButtonClicked);
            }

            if (!alreadyUnlocked && MergeCraftManager.I != null)
                MergeCraftManager.I.OnCraftSucceeded += OnCraftSucceeded;
        }

        void OnStageButtonClicked()
        {
            // 대화/컷신 중에는 스테이지 진입 금지
            if (DialogueManager.I != null && DialogueManager.I.IsPlaying) return;
            GameManager.I?.SetPendingSectionIndex(SectionIndex);
            CaptureSectionBackground();
        }

        // 이 노드가 속한 섹션 위치로 카메라를 임시 이동시켜 그 섹션의 맵 화면을 캡처한다.
        // (클릭 시점 카메라 위치/팝업과 무관하게 해당 섹션을 정확히 캡처)
        void CaptureSectionBackground()
        {
            var cam = Camera.main;
            if (cam == null || GameManager.I == null) return;

            Vector3 originalPos = cam.transform.position;
            var nav = ScreenNavigator.Instance;
            if (nav != null)
            {
                Vector3 secPos = nav.GetSectionPosition(SectionIndex);
                cam.transform.position = new Vector3(secPos.x, secPos.y, originalPos.z);
            }

            var tex = StageBackgroundCapture.Capture(cam);

            cam.transform.position = originalPos;   // 원위치 복원
            GameManager.I.SetPendingBackgroundTexture(tex);
        }

        void OnDestroy()
        {
            if (MergeCraftManager.I != null)
                MergeCraftManager.I.OnCraftSucceeded -= OnCraftSucceeded;
        }

        void OnCraftSucceeded(CharacterSO result)
        {
            if (result != unlockResult) return;
            if (unlockResult != null) SaveSystemManager.I?.UnlockChar(unlockResult.id);
            if (stageButton) stageButton.interactable = true;
        }
    }
}
