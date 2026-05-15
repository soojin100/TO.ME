using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TOME.Core;
using TOME.Data;
using TOME.Gameplay.Enemy;
using TOME.Gameplay.Player;
using TOME.UI;

namespace TOME.Managers
{
    /// 스테이지 씬 진입 후 라이프사이클 컨트롤.
    public class StageManager : MonoBehaviour
    {
        [SerializeField] PlayerShell      player;
        [SerializeField] Transform        playerSpawn;
        [SerializeField] List<RecipeSO>   recipes;
        [SerializeField] ItemDropManager  itemDropManager;
        [SerializeField] ResultScreenUI   resultScreen;

        IEnumerator Start()
        {
            Time.timeScale = 1f;
            var stage = GameManager.I?.CurrentStage;
            if (!stage) yield break;

            MergeCraftManager.I?.SetRecipes(recipes);
            InventoryManager.I?.Clear();
            EnemyRegistry.Clear();

            if (player)
            {
                if (playerSpawn) player.transform.position = playerSpawn.position;
                if (stage.startCharacter)
                    player.EquipCharacter(stage.startCharacter, GameManager.I.CurrentNode?.bonus);
            }

            yield return null;

            // 구독을 BeginStage 앞에 — 즉시 종료 조건이 생겨도 OnFinished 누락 방지
            if (CombatManager.I != null)     CombatManager.I.OnFinished        += OnFinished;
            if (MergeCraftManager.I != null) MergeCraftManager.I.OnCraftSucceeded += OnCrafted;
            if (player != null)              player.OnDied                     += OnPlayerDied;

            CombatManager.I?.BeginStage(stage);
            if (itemDropManager != null && stage.spawns != null && stage.spawns.Length > 0)
                itemDropManager.Begin(stage.spawns[0].enemy);
        }

        void OnPlayerDied()
        {
            CombatManager.I?.Finish(false);
        }

        void OnCrafted(CharacterSO ch)
        {
            if (player) player.EquipCharacter(ch, GameManager.I.CurrentNode?.bonus);
            CombatManager.I?.Resume();
            if (AudioManager.I != null) AudioManager.I.PlaySfx(AudioManager.I.dog2Sfx);
        }

        void OnFinished(bool win)
        {
            if (itemDropManager != null) itemDropManager.Stop();
            if (win) MapManager.I?.MarkNodeCleared(GameManager.I.CurrentNode);
            GameManager.I?.RecordStageResult(win);
            if (resultScreen) resultScreen.Show(win);
        }

        void OnDestroy()
        {
            if (CombatManager.I != null)     CombatManager.I.OnFinished        -= OnFinished;
            if (MergeCraftManager.I != null) MergeCraftManager.I.OnCraftSucceeded -= OnCrafted;
            if (player != null) player.OnDied -= OnPlayerDied;
        }
    }
}
