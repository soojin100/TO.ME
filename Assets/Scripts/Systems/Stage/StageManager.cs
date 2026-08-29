using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TOME.Data;
using TOME.Gameplay.Enemy;
using TOME.Gameplay.Player;
using TOME.UI;

namespace TOME.Systems
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
        [Tooltip("Stage 배경용 SpriteRenderer. GameManager.PendingBackgroundTexture(맵에서 캡처된 화면)가 sprite로 적용됨.")]
        [SerializeField] SpriteRenderer   backgroundSpriteRenderer;
        [Tooltip("배경 sprite에 강제 적용할 블러 머티리얼(SpriteBlurMaterial).")]
        [SerializeField] Material         backgroundBlurMaterial;
        [Tooltip("배경 sortingOrder. 적/플레이어보다 항상 뒤가 되도록 음수 큰 값.")]
        [SerializeField] int              backgroundSortingOrder = -1000;

        StageSO _stage;
        Sprite  _bgSprite;   // 런타임 생성 배경 sprite (OnDestroy에서 해제)

        IEnumerator Start()
        {
            // 초반 드랍 아이템 지정 코드
            Time.timeScale = 1f;
            var stage = GameManager.I != null ? GameManager.I.CurrentStage : null;
            if (!stage) stage = debugStage;
            if (!stage) yield break;
            _stage = stage;


            var node = GameManager.I != null ? GameManager.I.CurrentNode : null;
            ApplyStageBackground(stage);

            MergeCraftManager.I?.SetRecipes(recipes);
            EnemyRegistry.Clear();

            if (player)
            {
                if (playerSpawn) player.transform.position = playerSpawn.position;
                if (stage.startCharacter)
                    player.EquipCharacter(stage.startCharacter, node != null ? node.bonus : null);
            }

            // 초반 아이템 드랍 고정 코드
            GameManager.I?.TryGiveStarterItems();

            yield return null;

            // 구독을 BeginStage 앞에 — 즉시 종료 조건이 생겨도 OnFinished 누락 방지
            if (CombatManager.I != null)     CombatManager.I.OnFinished        += OnFinished;
            if (MergeCraftManager.I != null) MergeCraftManager.I.OnCraftSucceeded += OnCrafted;
            if (player != null)              player.OnDied                     += OnPlayerDied;

            CombatManager.I?.BeginStage(stage);
            if (itemDropManager != null && stage.spawns != null && stage.spawns.Length > 0)
                itemDropManager.Begin(stage.spawns[0].enemy);
        }

        // 스테이지 배경 = 맵에서 stageButton 클릭 시 캡처된 화면 텍스처를 sprite로 표시 + 블러.
        void ApplyStageBackground(StageSO stage)
        {
            if (backgroundSpriteRenderer == null)
            {
                Debug.LogWarning("[StageManager] backgroundSpriteRenderer is null.");
                return;
            }
            var tex = GameManager.I != null ? GameManager.I.PendingBackgroundTexture : null;
            if (tex == null)
            {
                Debug.LogWarning("[StageManager] PendingBackgroundTexture is null (Map에서 캡처가 호출되지 않았음).");
                backgroundSpriteRenderer.enabled = false;
                return;
            }

            // sprite를 카메라 시야 높이에 정확히 맞추는 pixelsPerUnit
            var cam = Camera.main;
            float orthoH = (cam != null && cam.orthographic) ? cam.orthographicSize * 2f : 10f;
            float pixelsPerUnit = tex.height / Mathf.Max(0.01f, orthoH);

            var sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height),
                                       new Vector2(0.5f, 0.5f), pixelsPerUnit);
            sprite.name = "StageBackground_Sprite";
            _bgSprite = sprite;

            backgroundSpriteRenderer.sprite = sprite;
            if (backgroundBlurMaterial != null) backgroundSpriteRenderer.sharedMaterial = backgroundBlurMaterial;
            backgroundSpriteRenderer.sortingOrder = backgroundSortingOrder;
            backgroundSpriteRenderer.enabled = true;
            Debug.Log($"[StageManager] Applied captured background ({tex.width}x{tex.height}, ppu={pixelsPerUnit:F2}).");
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
            // (제거) dog2Sfx는 효과음이 아니라 To.You의 긴 트랙(Dog2.mp3 2.8MB)이라 BGM과 겹쳐서 재생 안 함. 짧은 효과음 생기면 교체.
        }

        void OnFinished(bool win)
        {
            if (itemDropManager != null) itemDropManager.Stop();
            var node = GameManager.I != null ? GameManager.I.CurrentNode : null;
            if (win && node != null)
            {
                MapProgressionManager.I?.MarkNodeCleared(node);
                GameManager.I?.TryAdvanceChapter(node);   // 보스였다면 다음 챕터로
            }
            GameManager.I?.RecordStageResult(win);
            if (resultScreen) resultScreen.Show(win, _stage);
        }

        void OnDestroy()
        {
            if (CombatManager.I != null)     CombatManager.I.OnFinished        -= OnFinished;
            if (MergeCraftManager.I != null) MergeCraftManager.I.OnCraftSucceeded -= OnCrafted;
            if (player != null) player.OnDied -= OnPlayerDied;

            // 배경으로 쓴 런타임 sprite + 캡처 텍스처 해제(맵 복귀 시 메모리 반환).
            if (_bgSprite != null) { Destroy(_bgSprite); _bgSprite = null; }
            GameManager.I?.ClearPendingBackgroundTexture();
        }
    }
}
