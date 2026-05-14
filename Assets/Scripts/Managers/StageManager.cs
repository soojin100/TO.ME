using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TOME.Core;
using TOME.Data;
using TOME.Gameplay.Player;
using TOME.Gameplay.Merge;

namespace TOME.Managers
{
    /// 스테이지 씬 진입 후 라이프사이클 컨트롤.
    public class StageManager : MonoBehaviour
    {
        [SerializeField] PlayerShell         player;
        [SerializeField] Transform           playerSpawn;
        [SerializeField] List<RecipeSO>      recipes;     // 이 스테이지에서 가능한 레시피

        IEnumerator Start()
        {
            var stage = GameManager.I?.CurrentStage;
            if (!stage) yield break;

            RecipeMatcher.Init(recipes);
            InventoryManager.I?.Clear();

            // 시작 캐릭터 장착
            if (player && stage.startCharacter)
                player.EquipCharacter(stage.startCharacter, GameManager.I.CurrentNode?.bonus);

            // 대사 — 한 번만 출력 (DialogueManager 내부에서 체크)
            DialogueManager.I?.TryPlay(stage.preDialogueId);
            yield return null;

            // 전투 시작
            CombatManager.I?.BeginStage(stage);
            if (CombatManager.I != null)     CombatManager.I.OnFinished      += OnFinished;
            if (MergeCraftManager.I != null) MergeCraftManager.I.OnCraftSucceeded += OnCrafted;
        }

        void OnCrafted(CharacterSO ch)
        {
            if (player) player.EquipCharacter(ch, GameManager.I.CurrentNode?.bonus);
            CombatManager.I?.Resume();
        }

        void OnFinished(bool win)
        {
            if (win)
            {
                MapManager.I?.MarkNodeCleared(GameManager.I.CurrentNode);
                DialogueManager.I?.TryPlay(GameManager.I.CurrentStage.postDialogueId);
            }
        }

        void OnDestroy()
        {
            if (CombatManager.I != null) CombatManager.I.OnFinished -= OnFinished;
            if (MergeCraftManager.I != null) MergeCraftManager.I.OnCraftSucceeded -= OnCrafted;
        }
    }
}
