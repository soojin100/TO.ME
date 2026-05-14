using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TOME.Data;
using TOME.Managers;
using TOME.Gameplay.Merge;

namespace TOME.Map
{
    /// <summary>맵에서 조합을 성공하면 스테이지 버튼을 잠금 해제한다.</summary>
    public class MapStageGate : MonoBehaviour
    {
        [SerializeField] List<RecipeSO> recipes;       // 맵에서 가능한 조합 목록
        [SerializeField] CharacterSO    unlockResult;  // 이 결과물을 조합하면 스테이지 해제
        [SerializeField] Button         stageButton;   // 해제 대상 스테이지 버튼

        void Start()
        {
            RecipeMatcher.Init(recipes);
            if (stageButton) stageButton.interactable = false;
            if (MergeCraftManager.I != null)
                MergeCraftManager.I.OnCraftSucceeded += OnCraftSucceeded;
        }

        void OnDestroy()
        {
            if (MergeCraftManager.I != null)
                MergeCraftManager.I.OnCraftSucceeded -= OnCraftSucceeded;
        }

        void OnCraftSucceeded(CharacterSO result)
        {
            if (result == unlockResult && stageButton)
                stageButton.interactable = true;
        }
    }
}
