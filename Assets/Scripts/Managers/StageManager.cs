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
        [SerializeField] StageSO          debugStage;   // Stage 씬 단독 실행 테스트용 (Map 미경유 시 사용)

        IEnumerator Start()
        {
            Time.timeScale = 1f;
            // Map 경유면 GameManager.CurrentStage, 단독 실행이면 debugStage로 폴백
            var stage = GameManager.I != null ? GameManager.I.CurrentStage : null;
            if (!stage) stage = debugStage;
            if (!stage) yield break;

            var node = GameManager.I != null ? GameManager.I.CurrentNode : null;

            MergeCraftManager.I?.SetRecipes(recipes);
            InventoryManager.I?.Clear();
            EnemyRegistry.Clear();

            if (player)
            {
                if (playerSpawn) player.transform.position = playerSpawn.position;
                if (stage.startCharacter)
                    player.EquipCharacter(stage.startCharacter, node != null ? node.bonus : null);
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
            var node = GameManager.I != null ? GameManager.I.CurrentNode : null;
            if (player) player.EquipCharacter(ch, node != null ? node.bonus : null);
            CombatManager.I?.Resume();
            if (AudioManager.I != null) AudioManager.I.PlaySfx(AudioManager.I.dog2Sfx);
        }

        void OnFinished(bool win)
        {
            if (itemDropManager != null) itemDropManager.Stop();
            var node = GameManager.I != null ? GameManager.I.CurrentNode : null;
            if (win && node != null) MapManager.I?.MarkNodeCleared(node);
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
