# 스테이지1 수직 슬라이스 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** NewGame_TO.ME 기획서의 스테이지 1개를 맵→전투→조합→결과→복귀까지 실제 플레이 가능하게 완성한다.

**Architecture:** 기존 `TOME.*` 매니저/ScriptableObject 뼈대 위에 빠진 신규 스크립트(8개)를 채우고, 기존 스크립트를 최소 수정한 뒤, Unity Editor를 MCP로 조작해 ScriptableObject 에셋·프리팹·씬 3개를 생성·와이어링한다. DontDestroyOnLoad 매니저는 Boot 씬에, 씬 스코프 매니저는 각 씬에 배치.

**Tech Stack:** Unity 6000.4.0f1, URP 2D, C# (Assembly-CSharp + 신규 `TOME` asmdef), UGUI/TMP, Unity Test Framework, MCP for Unity 툴.

---

## 핵심 규약

- 모든 신규 스크립트 네임스페이스는 `TOME.*` 유지.
- 스크립트 작성/수정 후 **반드시 `mcp__UnityMCP__read_console`(filter: Error/Warning)로 컴파일 통과 확인** 후 다음 단계.
- 컴파일 대기: `mcp__UnityMCP__refresh_unity` 후 `editor_state` 리소스의 `isCompiling`이 false 될 때까지.
- Unity 경로는 `Assets/` 기준, 슬래시(`/`) 사용.
- 커밋은 각 Task 끝에서. 메시지는 한국어, `Co-Authored-By: Claude Opus 4.7 <noreply@anthropic.com>` 포함.

## File Structure

**신규 스크립트:**
- `Assets/Scripts/TOME.asmdef` — 메인 코드 어셈블리 정의
- `Assets/Scripts/Gameplay/Items/ItemPickup.cs` — 필드 아이템 픽업
- `Assets/Scripts/Managers/ItemDropManager.cs` — 적 사망/시간 드랍
- `Assets/Scripts/UI/DialogueUI.cs` — 대사 말풍선 UI
- `Assets/Scripts/UI/StageInfoPopupUI.cs` — 스테이지 정보 팝업
- `Assets/Scripts/UI/InventorySlotButton.cs` — 인벤토리 슬롯 버튼(리프)
- `Assets/Scripts/UI/InventoryBarUI.cs` — 인벤토리 바(페이징)
- `Assets/Scripts/UI/CraftSlotButton.cs` — 조합 슬롯 버튼(리프)
- `Assets/Scripts/UI/CraftPanelUI.cs` — 조합 패널
- `Assets/Scripts/UI/ResultScreenUI.cs` — 결과창
- `Assets/Scripts/Map/MapFlowController.cs` — 맵 흐름 제어
- `Assets/Tests/EditMode/TOME.Tests.EditMode.asmdef` — 테스트 어셈블리
- `Assets/Tests/EditMode/RecipeMatcherTests.cs` — 레시피 매칭 테스트

**수정 스크립트:**
- `Assets/Scripts/Managers/GameManager.cs` — 결과/사후대사 상태 + timeScale 리셋
- `Assets/Scripts/Managers/StageManager.cs` — 대사 호출 제거, 결과 기록, 드랍매니저 시동
- `Assets/Scripts/Managers/CombatManager.cs` — `OnEnemyKilled` 이벤트, Finish 시 timeScale=0
- `Assets/Scripts/Managers/DialogueManager.cs` — `SkipAll()`
- `Assets/Scripts/Data/CharacterSO.cs` — `bodyTint` 필드
- `Assets/Scripts/Data/StageSO.cs` — `introText`, `clearedIntroText`
- `Assets/Scripts/Gameplay/Player/CharacterCore.cs` — `bodyTint` 적용
- `Assets/Scripts/UI/Combat/HudUI.cs` — CraftPanelUI 연동 + 구독 해제
- `Assets/Scripts/Map/MapNode.cs` — MapFlowController로 위임

**신규 에셋:** ScriptableObject 16개, 프리팹 8개, 씬 3개.

---

## Task 1: 어셈블리 정의 추가 및 컴파일 베이스라인 확보

기존 스크립트는 asmdef가 없어 Assembly-CSharp에 컴파일된다. EditMode 테스트가 메인 코드를 참조하려면 메인 코드에 asmdef가 필요하다.

**Files:**
- Create: `Assets/Scripts/TOME.asmdef`

- [ ] **Step 1: TOME.asmdef 생성**

`mcp__UnityMCP__manage_asset` 또는 `create_script` 계열로 생성. 내용:

```json
{
    "name": "TOME",
    "rootNamespace": "TOME",
    "references": ["Unity.TextMeshPro"],
    "includePlatforms": [],
    "excludePlatforms": [],
    "autoReferenced": true
}
```

- [ ] **Step 2: 컴파일 확인**

`mcp__UnityMCP__refresh_unity` 후 `editor_state`의 `isCompiling`이 false 될 때까지 폴링 → `mcp__UnityMCP__read_console` (filter: Error).
Expected: 에러 0건. 만약 `Unity.TextMeshPro` 미해결 에러가 나면, 콘솔 메시지의 정확한 어셈블리명으로 `references` 교체 후 재확인. (Unity 6 / ugui 2.0에서 TMP 어셈블리명이 다를 수 있음)

- [ ] **Step 3: 커밋**

```bash
git add Assets/Scripts/TOME.asmdef Assets/Scripts/TOME.asmdef.meta
git commit -m "TOME 어셈블리 정의 추가

Co-Authored-By: Claude Opus 4.7 <noreply@anthropic.com>"
```

---

## Task 2: RecipeMatcher EditMode 테스트

조합 매칭이 재료 순서와 무관하게 동작하고, 미등록 조합은 null을 반환하는지 검증한다.

**Files:**
- Create: `Assets/Tests/EditMode/TOME.Tests.EditMode.asmdef`
- Create: `Assets/Tests/EditMode/RecipeMatcherTests.cs`

- [ ] **Step 1: 테스트 어셈블리 정의 생성**

`Assets/Tests/EditMode/TOME.Tests.EditMode.asmdef`:

```json
{
    "name": "TOME.Tests.EditMode",
    "references": ["TOME", "UnityEngine.TestRunner", "UnityEditor.TestRunner"],
    "includePlatforms": ["Editor"],
    "precompiledReferences": ["nunit.framework.dll"],
    "defineConstraints": ["UNITY_INCLUDE_TESTS"],
    "autoReferenced": false
}
```

- [ ] **Step 2: 실패하는 테스트 작성**

`Assets/Tests/EditMode/RecipeMatcherTests.cs`:

```csharp
using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;
using TOME.Data;
using TOME.Gameplay.Merge;

namespace TOME.Tests.EditMode
{
    public class RecipeMatcherTests
    {
        static ItemSO MakeItem(string id)
        {
            var it = ScriptableObject.CreateInstance<ItemSO>();
            it.id = id;
            return it;
        }

        static RecipeSO MakeRecipe(params ItemSO[] ingredients)
        {
            var r = ScriptableObject.CreateInstance<RecipeSO>();
            r.ingredients = ingredients;
            return r;
        }

        [Test]
        public void Match_IngredientOrderIndependent()
        {
            var potion = MakeItem("potion");
            var star = MakeItem("star");
            var recipe = MakeRecipe(potion, star);
            RecipeMatcher.Init(new[] { recipe });

            var matchAB = RecipeMatcher.Match(new List<ItemSO> { potion, star });
            var matchBA = RecipeMatcher.Match(new List<ItemSO> { star, potion });

            Assert.AreSame(recipe, matchAB);
            Assert.AreSame(recipe, matchBA);
        }

        [Test]
        public void Match_UnknownCombo_ReturnsNull()
        {
            var potion = MakeItem("potion");
            var star = MakeItem("star");
            var sword = MakeItem("sword");
            RecipeMatcher.Init(new[] { MakeRecipe(potion, star) });

            var result = RecipeMatcher.Match(new List<ItemSO> { potion, sword });

            Assert.IsNull(result);
        }
    }
}
```

- [ ] **Step 3: 테스트 실행하여 통과 확인**

`mcp__UnityMCP__run_tests` (mode: EditMode, filter: RecipeMatcherTests).
Expected: 2개 테스트 PASS. (RecipeMatcher 구현은 이미 존재하므로 바로 통과해야 정상 — 통과하지 않으면 RecipeMatcher 또는 asmdef 참조 문제이니 콘솔 확인 후 수정)

- [ ] **Step 4: 커밋**

```bash
git add Assets/Tests
git commit -m "RecipeMatcher EditMode 테스트 추가

Co-Authored-By: Claude Opus 4.7 <noreply@anthropic.com>"
```

---

## Task 3: 기존 데이터 스크립트 수정 (CharacterSO, StageSO)

플레이스홀더 캐릭터 색 구분과 스테이지 정보 팝업 텍스트를 위한 필드 추가.

**Files:**
- Modify: `Assets/Scripts/Data/CharacterSO.cs`
- Modify: `Assets/Scripts/Data/StageSO.cs`

- [ ] **Step 1: CharacterSO에 bodyTint 추가**

`CharacterSO.cs`의 `public Sprite icon;` 다음 줄에 추가:

```csharp
        public Color  bodyTint = Color.white;   // 플레이스홀더 캐릭터 시각 구분용
```

- [ ] **Step 2: StageSO에 introText, clearedIntroText 추가**

`StageSO.cs`의 `public Sprite thumbnail;` 다음 줄에 추가:

```csharp
        [TextArea] public string introText;          // 스테이지 정보 팝업 소개문
        [TextArea] public string clearedIntroText;   // 클리어 후 표시할 소개문
```

- [ ] **Step 3: 컴파일 확인**

`refresh_unity` → `read_console` (Error). Expected: 에러 0건.

- [ ] **Step 4: 커밋**

```bash
git add Assets/Scripts/Data/CharacterSO.cs Assets/Scripts/Data/StageSO.cs
git commit -m "CharacterSO bodyTint, StageSO 소개문 필드 추가

Co-Authored-By: Claude Opus 4.7 <noreply@anthropic.com>"
```

---

## Task 4: 기존 매니저/코어 스크립트 수정

대사 흐름을 맵으로 이전하고, 결과 기록·드랍 이벤트·timeScale 처리를 추가한다.

**Files:**
- Modify: `Assets/Scripts/Managers/GameManager.cs`
- Modify: `Assets/Scripts/Managers/CombatManager.cs`
- Modify: `Assets/Scripts/Managers/DialogueManager.cs`
- Modify: `Assets/Scripts/Managers/StageManager.cs`
- Modify: `Assets/Scripts/Gameplay/Player/CharacterCore.cs`

- [ ] **Step 1: GameManager 전체 교체**

`GameManager.cs` 전체를 아래로 교체:

```csharp
using System.Collections;
using UnityEngine;
using TOME.Core;
using TOME.Data;

namespace TOME.Managers
{
    public enum StageResult { None, Win, Lose }

    public class GameManager : MonoBehaviour
    {
        public static GameManager I { get; private set; }

        public NodeSO  CurrentNode  { get; private set; }
        public StageSO CurrentStage { get; private set; }
        public StageResult LastStageResult { get; private set; }
        public string PendingPostDialogueId { get; private set; }

        void Awake()
        {
            if (I != null && I != this) { Destroy(gameObject); return; }
            I = this; DontDestroyOnLoad(gameObject);
        }

        public void EnterStage(NodeSO node, StageSO stage)
        {
            Time.timeScale = 1f;
            CurrentNode  = node;
            CurrentStage = stage;
            LastStageResult = StageResult.None;
            PendingPostDialogueId = null;
            StartCoroutine(SceneLoader.LoadAsync(SceneKeys.Stage));
        }

        public void RecordStageResult(bool win)
        {
            LastStageResult = win ? StageResult.Win : StageResult.Lose;
            if (win && CurrentStage != null)
                PendingPostDialogueId = CurrentStage.postDialogueId;
        }

        public void ClearPendingPostDialogue() => PendingPostDialogueId = null;

        public void ReturnToMap()
        {
            Time.timeScale = 1f;
            StartCoroutine(SceneLoader.LoadAsync(SceneKeys.Map));
        }
    }
}
```

- [ ] **Step 2: CombatManager에 OnEnemyKilled 이벤트 추가**

`CombatManager.cs`에서 `public event Action<bool> OnFinished;` 다음 줄에 추가:

```csharp
        public event Action<EnemySO,Vector3> OnEnemyKilled;
```

`OnEnemyDied` 메서드 전체를 아래로 교체:

```csharp
        void OnEnemyDied(EnemyBase e)
        {
            AliveOnField    = Mathf.Max(0, AliveOnField - 1);
            RemainingToKill = Mathf.Max(0, RemainingToKill - 1);
            OnCountChanged?.Invoke(RemainingToKill, TotalEnemies);

            var go = e.gameObject;
            if (instToDef.TryGetValue(go, out var def))
            {
                OnEnemyKilled?.Invoke(def, go.transform.position);
                if (pools.TryGetValue(def, out var pool)) pool.Release(go);
                else go.SetActive(false);
            }
            else go.SetActive(false);

            if (RemainingToKill == 0) Finish(true);
        }
```

`Finish` 메서드 전체를 아래로 교체 (종료 시 게임 정지):

```csharp
        void Finish(bool win)
        {
            if (IsFinished) return;
            IsFinished = true;
            Time.timeScale = 0f;
            OnFinished?.Invoke(win);
        }
```

- [ ] **Step 3: DialogueManager에 SkipAll 추가**

`DialogueManager.cs`에 `bool _advance;` 다음 줄에 필드 추가:

```csharp
        bool _skip;
```

`Advance` 메서드 다음에 추가:

```csharp
        /// <summary>스킵 버튼: 현재 대사 시퀀스를 즉시 종료.</summary>
        public void SkipAll()
        {
            if (!IsPlaying) return;
            _skip = true;
            _advance = true;
        }
```

`Run` 코루틴 전체를 아래로 교체 (`_skip` 처리):

```csharp
        IEnumerator Run(string startId)
        {
            IsPlaying = true;
            _skip = false;
            string cur = startId;
            while (!_skip && !string.IsNullOrEmpty(cur) && table.TryGetValue(cur, out var e))
            {
                OnLine?.Invoke(e);
                _advance = false;
                while (!_advance) yield return null;
                cur = e.next;
            }
            IsPlaying = false;
            _skip = false;
            SaveSystemManager.I?.MarkDialogueSeen(startId);
            OnEnd?.Invoke();
        }
```

- [ ] **Step 4: CharacterCore에 bodyTint 적용**

`CharacterCore.cs`의 `Bind` 메서드 전체를 아래로 교체:

```csharp
        public void Bind(CharacterSO def)
        {
            Def = def;
            if (body)
            {
                if (def.icon) body.sprite = def.icon;
                body.color = def.bodyTint;
            }
            if (animator && !string.IsNullOrEmpty(idleStateName)) animator.Play(idleStateName, 0, 0f);
        }
```

- [ ] **Step 5: StageManager 전체 교체**

`StageManager.cs` 전체를 아래로 교체 (대사 호출 제거, 결과 기록, 드랍매니저·결과창 연동):

```csharp
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TOME.Core;
using TOME.Data;
using TOME.Gameplay.Player;
using TOME.Gameplay.Merge;
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
            var stage = GameManager.I?.CurrentStage;
            if (!stage) yield break;

            RecipeMatcher.Init(recipes);
            InventoryManager.I?.Clear();

            if (player)
            {
                if (playerSpawn) player.transform.position = playerSpawn.position;
                if (stage.startCharacter)
                    player.EquipCharacter(stage.startCharacter, GameManager.I.CurrentNode?.bonus);
            }

            yield return null;

            CombatManager.I?.BeginStage(stage);
            if (itemDropManager != null && stage.spawns != null && stage.spawns.Length > 0)
                itemDropManager.Begin(stage.spawns[0].enemy);

            if (CombatManager.I != null)     CombatManager.I.OnFinished        += OnFinished;
            if (MergeCraftManager.I != null) MergeCraftManager.I.OnCraftSucceeded += OnCrafted;
        }

        void OnCrafted(CharacterSO ch)
        {
            if (player) player.EquipCharacter(ch, GameManager.I.CurrentNode?.bonus);
            CombatManager.I?.Resume();
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
        }
    }
}
```

> 주의: `StageManager`는 `ResultScreenUI`(Task 8)와 `ItemDropManager`(Task 5)를 참조한다. 이 단계에서는 두 타입이 아직 없어 컴파일 에러가 난다. **Task 5와 Task 8 완료 후 Step 6 컴파일 확인을 수행**하거나, Task 순서를 5·8을 먼저 진행한 뒤 본 Step 5를 적용한다. 권장: 본 Step 5를 제외한 Step 1~4를 먼저 커밋(아래 Step 6a)하고, Step 5는 Task 8 직후에 적용·커밋(Step 6b).

- [ ] **Step 6a: Step 1~4 컴파일 확인 및 커밋**

`refresh_unity` → `read_console` (Error). Expected: 에러 0건.

```bash
git add Assets/Scripts/Managers/GameManager.cs Assets/Scripts/Managers/CombatManager.cs Assets/Scripts/Managers/DialogueManager.cs Assets/Scripts/Gameplay/Player/CharacterCore.cs
git commit -m "매니저 수정: 결과 기록, 적 처치 이벤트, 대사 스킵, timeScale 처리

Co-Authored-By: Claude Opus 4.7 <noreply@anthropic.com>"
```

- [ ] **Step 6b: (Task 8 이후) StageManager 적용·확인·커밋**

Task 8 완료 후 본 Task의 Step 5를 적용하고:

`refresh_unity` → `read_console` (Error). Expected: 에러 0건.

```bash
git add Assets/Scripts/Managers/StageManager.cs
git commit -m "StageManager: 대사 호출 제거, 결과창·드랍매니저 연동

Co-Authored-By: Claude Opus 4.7 <noreply@anthropic.com>"
```

---

## Task 5: ItemPickup + ItemDropManager

필드 아이템과 드랍 시스템.

**Files:**
- Create: `Assets/Scripts/Gameplay/Items/ItemPickup.cs`
- Create: `Assets/Scripts/Managers/ItemDropManager.cs`

- [ ] **Step 1: ItemPickup 작성**

`Assets/Scripts/Gameplay/Items/ItemPickup.cs`:

```csharp
using UnityEngine;
using TOME.Core;
using TOME.Data;
using TOME.Managers;
using TOME.Gameplay.Player;

namespace TOME.Gameplay.Items
{
    /// <summary>필드 아이템. 스폰 후 수집 영역으로 이동, 플레이어 충돌 시 인벤토리 획득.</summary>
    [RequireComponent(typeof(SpriteRenderer), typeof(CircleCollider2D))]
    public class ItemPickup : MonoBehaviour
    {
        SpriteRenderer _sr;
        Transform _tr;
        ItemSO _def;
        ObjectPool _pool;
        float _driftSpeed;
        float _targetY;
        bool _active;

        void Awake()
        {
            _tr = transform;
            _sr = GetComponent<SpriteRenderer>();
        }

        public void Init(ItemSO def, Vector3 spawnPos, float targetY, float driftSpeed, ObjectPool pool)
        {
            _def = def;
            _pool = pool;
            _targetY = targetY;
            _driftSpeed = driftSpeed;
            _tr.position = spawnPos;
            if (_sr && def != null && def.icon) _sr.sprite = def.icon;
            _active = true;
        }

        void Update()
        {
            if (!_active) return;
            Vector3 p = _tr.position;
            p.y = Mathf.MoveTowards(p.y, _targetY, _driftSpeed * Time.deltaTime);
            _tr.position = p;
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (!_active || _def == null) return;
            if (!other.GetComponentInParent<PlayerShell>()) return;
            InventoryManager.I?.Add(_def);
            Despawn();
        }

        void Despawn()
        {
            _active = false;
            if (_pool != null) _pool.Release(gameObject);
            else gameObject.SetActive(false);
        }
    }
}
```

- [ ] **Step 2: ItemDropManager 작성**

`Assets/Scripts/Managers/ItemDropManager.cs`:

```csharp
using System.Collections;
using UnityEngine;
using TOME.Core;
using TOME.Data;
using TOME.Gameplay.Items;

namespace TOME.Managers
{
    /// <summary>적 사망 콜백 + 주기 타이머로 필드에 ItemPickup 스폰 (풀링).</summary>
    public class ItemDropManager : MonoBehaviour
    {
        [SerializeField] GameObject itemPickupPrefab;
        [SerializeField] Transform  itemRoot;
        [SerializeField] float collectionY      = -7f;
        [SerializeField] float driftSpeed       = 2.5f;
        [SerializeField] float timedDropInterval = 6f;
        [SerializeField] Vector2 timedSpawnXRange = new(-2.5f, 2.5f);
        [SerializeField] float timedSpawnY       = 3f;
        [SerializeField] int   prewarm           = 6;

        ObjectPool _pool;
        EnemySO    _timedSource;
        Coroutine  _timedLoop;
        bool       _active;

        void Awake()
        {
            if (itemPickupPrefab)
                _pool = new ObjectPool(itemPickupPrefab, prewarm, itemRoot);
        }

        public void Begin(EnemySO timedDropSource)
        {
            _timedSource = timedDropSource;
            _active = true;
            if (CombatManager.I != null) CombatManager.I.OnEnemyKilled += OnEnemyKilled;
            _timedLoop = StartCoroutine(TimedDropLoop());
        }

        public void Stop()
        {
            _active = false;
            if (CombatManager.I != null) CombatManager.I.OnEnemyKilled -= OnEnemyKilled;
            if (_timedLoop != null) { StopCoroutine(_timedLoop); _timedLoop = null; }
        }

        void OnDestroy() => Stop();

        void OnEnemyKilled(EnemySO def, Vector3 pos)
        {
            if (!_active || def == null) return;
            if (Random.value > def.dropChance) return;
            var item = PickItem(def);
            if (item) SpawnPickup(item, pos);
        }

        IEnumerator TimedDropLoop()
        {
            var wait = new WaitForSeconds(timedDropInterval);
            while (_active)
            {
                yield return wait;
                if (!_active || _timedSource == null) continue;
                if (CombatManager.I != null && (CombatManager.I.IsFinished || CombatManager.I.IsPaused)) continue;
                var item = PickItem(_timedSource);
                if (item)
                {
                    float x = Random.Range(timedSpawnXRange.x, timedSpawnXRange.y);
                    SpawnPickup(item, new Vector3(x, timedSpawnY, 0f));
                }
            }
        }

        ItemSO PickItem(EnemySO def)
        {
            if (def.dropTable == null || def.dropTable.Length == 0) return null;
            if (def.dropWeights == null || def.dropWeights.Length != def.dropTable.Length)
                return def.dropTable[Random.Range(0, def.dropTable.Length)];

            float total = 0f;
            for (int i = 0; i < def.dropWeights.Length; i++) total += def.dropWeights[i];
            if (total <= 0f) return def.dropTable[Random.Range(0, def.dropTable.Length)];

            float r = Random.value * total;
            for (int i = 0; i < def.dropTable.Length; i++)
            {
                r -= def.dropWeights[i];
                if (r <= 0f) return def.dropTable[i];
            }
            return def.dropTable[def.dropTable.Length - 1];
        }

        void SpawnPickup(ItemSO item, Vector3 pos)
        {
            if (_pool == null) return;
            var go = _pool.Get(pos, Quaternion.identity);
            if (go.TryGetComponent<ItemPickup>(out var pick))
                pick.Init(item, pos, collectionY, driftSpeed, _pool);
        }
    }
}
```

- [ ] **Step 3: 컴파일 확인**

`refresh_unity` → `read_console` (Error). Expected: 에러 0건.

- [ ] **Step 4: 커밋**

```bash
git add Assets/Scripts/Gameplay/Items Assets/Scripts/Managers/ItemDropManager.cs
git commit -m "아이템 픽업 및 드랍 매니저 추가

Co-Authored-By: Claude Opus 4.7 <noreply@anthropic.com>"
```

---

## Task 6: DialogueUI

대사 말풍선 + 탭 진행 + 스킵.

**Files:**
- Create: `Assets/Scripts/UI/DialogueUI.cs`

- [ ] **Step 1: DialogueUI 작성**

`Assets/Scripts/UI/DialogueUI.cs`:

```csharp
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using TOME.Data;
using TOME.Managers;

namespace TOME.UI
{
    /// <summary>DialogueManager 구독. 말풍선 표시 + 클릭 진행 + 스킵.</summary>
    public class DialogueUI : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] GameObject root;
        [SerializeField] TMP_Text  speakerLabel;
        [SerializeField] TMP_Text  textLabel;
        [SerializeField] Button    skipButton;

        void Awake()
        {
            if (root) root.SetActive(false);
            if (skipButton) skipButton.onClick.AddListener(OnSkip);
        }

        void OnEnable()
        {
            if (DialogueManager.I != null)
            {
                DialogueManager.I.OnLine += OnLine;
                DialogueManager.I.OnEnd  += OnEnd;
            }
        }

        void OnDisable()
        {
            if (DialogueManager.I != null)
            {
                DialogueManager.I.OnLine -= OnLine;
                DialogueManager.I.OnEnd  -= OnEnd;
            }
        }

        void OnLine(DialogueEntry e)
        {
            if (root) root.SetActive(true);
            if (speakerLabel) speakerLabel.text = e.speaker;
            if (textLabel)    textLabel.text    = e.text;
        }

        void OnEnd()
        {
            if (root) root.SetActive(false);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            DialogueManager.I?.Advance();
        }

        void OnSkip()
        {
            DialogueManager.I?.SkipAll();
        }
    }
}
```

- [ ] **Step 2: 컴파일 확인**

`refresh_unity` → `read_console` (Error). Expected: 에러 0건.

- [ ] **Step 3: 커밋**

```bash
git add Assets/Scripts/UI/DialogueUI.cs
git commit -m "대사 UI 추가

Co-Authored-By: Claude Opus 4.7 <noreply@anthropic.com>"
```

---

## Task 7: StageInfoPopupUI

스테이지 정보 팝업.

**Files:**
- Create: `Assets/Scripts/UI/StageInfoPopupUI.cs`

- [ ] **Step 1: StageInfoPopupUI 작성**

`Assets/Scripts/UI/StageInfoPopupUI.cs`:

```csharp
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TOME.Core;
using TOME.Data;
using TOME.Managers;

namespace TOME.UI
{
    /// <summary>맵에서 노드 선택 시 표시. 클리어 여부에 따라 텍스트 분기.</summary>
    public class StageInfoPopupUI : MonoBehaviour
    {
        [SerializeField] GameObject root;
        [SerializeField] Image      thumbnail;
        [SerializeField] TMP_Text   characterNameLabel;
        [SerializeField] TMP_Text   introLabel;
        [SerializeField] GameObject clearBadge;
        [SerializeField] Button     startButton;
        [SerializeField] Button     closeButton;

        NodeSO  _node;
        StageSO _stage;

        void Awake()
        {
            if (root) root.SetActive(false);
            if (startButton) startButton.onClick.AddListener(OnStart);
            if (closeButton) closeButton.onClick.AddListener(Hide);
        }

        public void Show(NodeSO node, StageSO stage)
        {
            if (node == null || stage == null) return;
            _node  = node;
            _stage = stage;

            bool cleared = SaveSystemManager.I != null && SaveSystemManager.I.IsNodeCleared(node.id);

            if (root) root.SetActive(true);
            if (thumbnail)
            {
                thumbnail.enabled = stage.thumbnail != null;
                if (stage.thumbnail) thumbnail.sprite = stage.thumbnail;
            }
            if (characterNameLabel)
                characterNameLabel.text = stage.startCharacter ? stage.startCharacter.displayName : stage.title;
            if (introLabel)
            {
                string txt = cleared && !string.IsNullOrEmpty(stage.clearedIntroText)
                    ? stage.clearedIntroText : stage.introText;
                introLabel.text = txt;
            }
            if (clearBadge) clearBadge.SetActive(cleared);
        }

        public void Hide()
        {
            if (root) root.SetActive(false);
        }

        void OnStart()
        {
            Hide();
            if (_node && _stage) GameManager.I?.EnterStage(_node, _stage);
        }
    }
}
```

- [ ] **Step 2: 컴파일 확인**

`refresh_unity` → `read_console` (Error). Expected: 에러 0건.

- [ ] **Step 3: 커밋**

```bash
git add Assets/Scripts/UI/StageInfoPopupUI.cs
git commit -m "스테이지 정보 팝업 UI 추가

Co-Authored-By: Claude Opus 4.7 <noreply@anthropic.com>"
```

---

## Task 8: 조합/인벤토리/결과 UI

인벤토리 바, 조합 패널, 결과창, HudUI 수정.

**Files:**
- Create: `Assets/Scripts/UI/InventorySlotButton.cs`
- Create: `Assets/Scripts/UI/InventoryBarUI.cs`
- Create: `Assets/Scripts/UI/CraftSlotButton.cs`
- Create: `Assets/Scripts/UI/CraftPanelUI.cs`
- Create: `Assets/Scripts/UI/ResultScreenUI.cs`
- Modify: `Assets/Scripts/UI/Combat/HudUI.cs`

- [ ] **Step 1: InventorySlotButton 작성**

`Assets/Scripts/UI/InventorySlotButton.cs`:

```csharp
using System;
using UnityEngine;
using UnityEngine.UI;
using TOME.Data;

namespace TOME.UI
{
    /// <summary>인벤토리 바 슬롯. 아이콘 표시 + 클릭 콜백.</summary>
    [RequireComponent(typeof(Button))]
    public class InventorySlotButton : MonoBehaviour
    {
        [SerializeField] Image icon;

        Button _btn;
        ItemSO _item;
        Action<ItemSO> _cb;

        void Awake()
        {
            _btn = GetComponent<Button>();
            _btn.onClick.AddListener(() => { if (_item != null) _cb?.Invoke(_item); });
        }

        public void Bind(ItemSO item, float iconScale, Action<ItemSO> cb)
        {
            _item = item;
            _cb   = cb;
            if (icon)
            {
                icon.enabled = item != null && item.icon != null;
                if (item != null && item.icon) icon.sprite = item.icon;
                icon.transform.localScale = Vector3.one * iconScale;
            }
            _btn.interactable = item != null;
        }

        public void Clear()
        {
            _item = null;
            _cb   = null;
            if (icon) icon.enabled = false;
            _btn.interactable = false;
        }
    }
}
```

- [ ] **Step 2: InventoryBarUI 작성**

`Assets/Scripts/UI/InventoryBarUI.cs`:

```csharp
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TOME.Data;
using TOME.Managers;

namespace TOME.UI
{
    /// <summary>InventoryManager 바인딩. 가로 페이징, 아이콘 30% 축소 표시.</summary>
    public class InventoryBarUI : MonoBehaviour
    {
        [SerializeField] Transform  slotContainer;
        [SerializeField] GameObject slotButtonPrefab;   // InventorySlotButton
        [SerializeField] Button     prevButton;
        [SerializeField] Button     nextButton;
        [SerializeField] int        visibleCount = 4;
        [SerializeField] float      iconScale    = 0.7f;

        public event Action<ItemSO> OnItemClicked;

        readonly List<InventorySlotButton> _buttons = new();
        int _page;
        bool _wired;

        void OnEnable()
        {
            if (InventoryManager.I != null) InventoryManager.I.OnChanged += Refresh;
            if (!_wired)
            {
                if (prevButton) prevButton.onClick.AddListener(PrevPage);
                if (nextButton) nextButton.onClick.AddListener(NextPage);
                _wired = true;
            }
            _page = 0;
            Refresh();
        }

        void OnDisable()
        {
            if (InventoryManager.I != null) InventoryManager.I.OnChanged -= Refresh;
        }

        void EnsureButtons()
        {
            while (_buttons.Count < visibleCount && slotButtonPrefab && slotContainer)
            {
                var go = Instantiate(slotButtonPrefab, slotContainer);
                if (go.TryGetComponent<InventorySlotButton>(out var sb))
                    _buttons.Add(sb);
                else { Destroy(go); break; }
            }
        }

        public void Refresh()
        {
            EnsureButtons();
            var items = InventoryManager.I != null ? InventoryManager.I.Items : null;
            int count = items?.Count ?? 0;
            int maxPage = Mathf.Max(0, (count - 1) / Mathf.Max(1, visibleCount));
            _page = Mathf.Clamp(_page, 0, maxPage);
            int start = _page * visibleCount;

            for (int i = 0; i < _buttons.Count; i++)
            {
                int idx = start + i;
                if (items != null && idx < count)
                    _buttons[i].Bind(items[idx], iconScale, OnSlotClicked);
                else
                    _buttons[i].Clear();
            }
            if (prevButton) prevButton.interactable = _page > 0;
            if (nextButton) nextButton.interactable = _page < maxPage;
        }

        void OnSlotClicked(ItemSO item) => OnItemClicked?.Invoke(item);
        void PrevPage() { _page = Mathf.Max(0, _page - 1); Refresh(); }
        void NextPage() { _page++; Refresh(); }
    }
}
```

- [ ] **Step 3: CraftSlotButton 작성**

`Assets/Scripts/UI/CraftSlotButton.cs`:

```csharp
using System;
using UnityEngine;
using UnityEngine.UI;
using TOME.Data;

namespace TOME.UI
{
    /// <summary>조합창 슬롯. 인덱스 고정, 클릭 시 인덱스 콜백.</summary>
    [RequireComponent(typeof(Button))]
    public class CraftSlotButton : MonoBehaviour
    {
        [SerializeField] Image icon;

        Button _btn;
        int _index;
        Action<int> _cb;

        void Awake() { _btn = GetComponent<Button>(); }

        public void Init(int index, Action<int> onClick)
        {
            _index = index;
            _cb = onClick;
            _btn.onClick.AddListener(() => _cb?.Invoke(_index));
        }

        public void Bind(ItemSO item)
        {
            if (icon)
            {
                icon.enabled = item != null && item.icon != null;
                if (item != null && item.icon) icon.sprite = item.icon;
            }
        }
    }
}
```

- [ ] **Step 4: CraftPanelUI 작성**

`Assets/Scripts/UI/CraftPanelUI.cs`:

```csharp
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TOME.Data;
using TOME.Managers;
using TOME.UI.Combat;

namespace TOME.UI
{
    /// <summary>4슬롯 조합창 + 결과창. MergeCraftManager 바인딩.</summary>
    public class CraftPanelUI : MonoBehaviour
    {
        [SerializeField] GameObject       root;
        [SerializeField] InventoryBarUI   inventoryBar;
        [SerializeField] CraftSlotButton[] craftSlots;   // 4개
        [SerializeField] Button           resultButton;
        [SerializeField] Image            resultIcon;
        [SerializeField] TMP_Text         resultLabel;
        [SerializeField] Button           closeButton;
        [SerializeField] HudUI            hud;

        bool _initialized;

        void Awake()
        {
            if (root) root.SetActive(false);
            for (int i = 0; i < craftSlots.Length; i++)
            {
                int idx = i;
                craftSlots[i].Init(idx, OnCraftSlotClicked);
            }
            if (resultButton) resultButton.onClick.AddListener(OnResultClicked);
            if (closeButton)  closeButton.onClick.AddListener(OnClose);
            _initialized = true;
        }

        void OnEnable()
        {
            if (inventoryBar) inventoryBar.OnItemClicked += OnInventoryItemClicked;
            if (MergeCraftManager.I != null) MergeCraftManager.I.OnSlotsChanged += RefreshSlots;
        }

        void OnDisable()
        {
            if (inventoryBar) inventoryBar.OnItemClicked -= OnInventoryItemClicked;
            if (MergeCraftManager.I != null) MergeCraftManager.I.OnSlotsChanged -= RefreshSlots;
        }

        /// <summary>HudUI가 인벤토리 버튼 클릭 시 호출.</summary>
        public void Open()
        {
            if (root) root.SetActive(true);
            RefreshSlots();
            if (inventoryBar) inventoryBar.Refresh();
        }

        void OnClose()
        {
            // 조합창에 올린 아이템은 인벤토리로 회수
            if (MergeCraftManager.I != null)
                for (int i = 0; i < MergeCraftManager.SlotCount; i++)
                    MergeCraftManager.I.ReturnSlotToInventory(i);
            if (root) root.SetActive(false);
            if (hud) hud.OnClickCloseCraft();
        }

        void OnInventoryItemClicked(ItemSO item)
        {
            if (MergeCraftManager.I == null) return;
            for (int i = 0; i < MergeCraftManager.SlotCount; i++)
            {
                if (MergeCraftManager.I.GetSlot(i) == null)
                {
                    MergeCraftManager.I.PlaceFromInventory(i, item);
                    break;
                }
            }
        }

        void OnCraftSlotClicked(int idx)
        {
            MergeCraftManager.I?.ReturnSlotToInventory(idx);
        }

        void RefreshSlots()
        {
            if (MergeCraftManager.I == null) return;
            for (int i = 0; i < craftSlots.Length; i++)
                craftSlots[i].Bind(MergeCraftManager.I.GetSlot(i));

            var preview = MergeCraftManager.I.Preview();
            if (resultIcon)
            {
                resultIcon.enabled = preview != null && preview.icon != null;
                if (preview != null && preview.icon) resultIcon.sprite = preview.icon;
            }
            if (resultLabel)  resultLabel.text = preview != null ? preview.displayName : "";
            if (resultButton) resultButton.interactable = preview != null;
        }

        void OnResultClicked()
        {
            if (MergeCraftManager.I != null && MergeCraftManager.I.Craft())
            {
                // 조합 성공: StageManager.OnCrafted가 캐릭터 교체 + Resume 처리.
                if (root) root.SetActive(false);
            }
        }
    }
}
```

- [ ] **Step 5: ResultScreenUI 작성**

`Assets/Scripts/UI/ResultScreenUI.cs`:

```csharp
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TOME.Data;
using TOME.Managers;
using TOME.Gameplay.Player;

namespace TOME.UI
{
    /// <summary>승패 결과창. 캐릭터 중앙 이동 → CLEAR/FAIL → 2초 후 돌아가기 버튼.</summary>
    public class ResultScreenUI : MonoBehaviour
    {
        [SerializeField] GameObject  root;
        [SerializeField] GameObject  clearGraphic;
        [SerializeField] GameObject  failGraphic;
        [SerializeField] GameObject  starsGroup;
        [SerializeField] Transform   rewardContainer;
        [SerializeField] GameObject  rewardIconPrefab;   // Image + TMP_Text
        [SerializeField] Button      returnButton;
        [SerializeField] PlayerShell player;
        [SerializeField] Transform   centerAnchor;
        [SerializeField] float       moveDuration = 0.5f;
        [SerializeField] float       showDelay    = 2f;

        void Awake()
        {
            if (root) root.SetActive(false);
            if (returnButton)
            {
                returnButton.onClick.AddListener(OnReturn);
                returnButton.gameObject.SetActive(false);
            }
        }

        public void Show(bool win)
        {
            StartCoroutine(ShowRoutine(win));
        }

        IEnumerator ShowRoutine(bool win)
        {
            if (root)         root.SetActive(true);
            if (clearGraphic) clearGraphic.SetActive(win);
            if (failGraphic)  failGraphic.SetActive(!win);
            if (starsGroup)   starsGroup.SetActive(win);

            if (player && centerAnchor)
            {
                Transform pt = player.transform;
                Vector3 from = pt.position;
                Vector3 to   = centerAnchor.position;
                float t = 0f;
                while (t < moveDuration)
                {
                    t += Time.unscaledDeltaTime;
                    pt.position = Vector3.Lerp(from, to, t / moveDuration);
                    yield return null;
                }
                pt.position = to;
            }

            if (win) ShowRewards();

            yield return new WaitForSecondsRealtime(showDelay);
            if (returnButton) returnButton.gameObject.SetActive(true);
        }

        void ShowRewards()
        {
            var stage = GameManager.I != null ? GameManager.I.CurrentStage : null;
            if (stage == null || stage.rewards == null || rewardContainer == null || rewardIconPrefab == null)
                return;

            foreach (var r in stage.rewards)
            {
                if (!r) continue;
                var go = Instantiate(rewardIconPrefab, rewardContainer);
                var img   = go.GetComponentInChildren<Image>();
                var label = go.GetComponentInChildren<TMP_Text>();
                if (r.type == RewardType.Item && r.item)
                {
                    if (img)   { img.enabled = r.item.icon != null; if (r.item.icon) img.sprite = r.item.icon; }
                    if (label) label.text = $"x{r.amount}";
                }
                else if (r.type == RewardType.Coin)
                {
                    if (img)   img.enabled = false;
                    if (label) label.text = $"+{r.amount}";
                }
                else if (r.type == RewardType.Character && r.character)
                {
                    if (img)   { img.enabled = r.character.icon != null; if (r.character.icon) img.sprite = r.character.icon; }
                    if (label) label.text = r.character.displayName;
                }
            }
        }

        void OnReturn()
        {
            if (root) root.SetActive(false);
            GameManager.I?.ReturnToMap();
        }
    }
}
```

- [ ] **Step 6: HudUI 전체 교체**

`Assets/Scripts/UI/Combat/HudUI.cs` 전체를 아래로 교체:

```csharp
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TOME.Managers;
using TOME.Gameplay.Player;

namespace TOME.UI.Combat
{
    /// 상단 적남은수·타이머·HP, 하단 인벤토리 진입 버튼.
    public class HudUI : MonoBehaviour
    {
        [SerializeField] TMP_Text    countLabel;
        [SerializeField] TMP_Text    timerLabel;
        [SerializeField] Slider      hpBar;
        [SerializeField] PlayerShell player;
        [SerializeField] TOME.UI.CraftPanelUI craftPanel;

        void Start()
        {
            if (CombatManager.I != null)
            {
                CombatManager.I.OnCountChanged += OnCount;
                CombatManager.I.OnTimerChanged += OnTimer;
            }
            if (player) player.OnHpChanged += OnHp;
        }

        void OnDestroy()
        {
            if (CombatManager.I != null)
            {
                CombatManager.I.OnCountChanged -= OnCount;
                CombatManager.I.OnTimerChanged -= OnTimer;
            }
            if (player) player.OnHpChanged -= OnHp;
        }

        void OnCount(int rem, int tot) { if (countLabel) countLabel.text = $"남은적 {tot - rem}/{tot}"; }
        void OnTimer(float t)          { if (timerLabel) timerLabel.text = $"{Mathf.CeilToInt(Mathf.Max(0f, t))}s"; }
        void OnHp(int hp, int max)     { if (hpBar) hpBar.value = (float)hp / Mathf.Max(1, max); }

        /// 하단 인벤토리 버튼 OnClick에 연결.
        public void OnClickInventory()
        {
            CombatManager.I?.Pause();
            if (craftPanel) craftPanel.Open();
        }

        /// CraftPanelUI가 닫힐 때 호출.
        public void OnClickCloseCraft()
        {
            CombatManager.I?.Resume();
        }
    }
}
```

- [ ] **Step 7: 컴파일 확인**

`refresh_unity` → `read_console` (Error). Expected: 에러 0건. (이 시점에서 Task 4 Step 5의 StageManager가 아직 미적용이면 정상 — StageManager는 다음 단계에서 적용)

- [ ] **Step 8: Task 4 Step 5·6b 적용**

Task 4의 Step 5(StageManager 전체 교체)를 지금 적용하고 Step 6b(컴파일 확인 + 커밋)를 수행한다.

- [ ] **Step 9: 커밋**

```bash
git add Assets/Scripts/UI/InventorySlotButton.cs Assets/Scripts/UI/InventoryBarUI.cs Assets/Scripts/UI/CraftSlotButton.cs Assets/Scripts/UI/CraftPanelUI.cs Assets/Scripts/UI/ResultScreenUI.cs Assets/Scripts/UI/Combat/HudUI.cs
git commit -m "조합/인벤토리/결과 UI 추가, HudUI 연동

Co-Authored-By: Claude Opus 4.7 <noreply@anthropic.com>"
```

---

## Task 9: MapFlowController + MapNode 수정

맵 흐름 제어.

**Files:**
- Create: `Assets/Scripts/Map/MapFlowController.cs`
- Modify: `Assets/Scripts/Map/MapNode.cs`

- [ ] **Step 1: MapFlowController 작성**

`Assets/Scripts/Map/MapFlowController.cs`:

```csharp
using UnityEngine;
using TOME.Data;
using TOME.Managers;
using TOME.UI;

namespace TOME.Map
{
    /// <summary>노드 선택 → 사전 대사 → 팝업. 맵 진입 시 사후 대사 재생.</summary>
    public class MapFlowController : MonoBehaviour
    {
        [SerializeField] StageInfoPopupUI stagePopup;

        NodeSO  _pendingNode;
        StageSO _pendingStage;
        bool    _waitingForPreDialogue;

        void OnEnable()
        {
            if (DialogueManager.I != null) DialogueManager.I.OnEnd += OnDialogueEnd;
        }

        void OnDisable()
        {
            if (DialogueManager.I != null) DialogueManager.I.OnEnd -= OnDialogueEnd;
        }

        void Start()
        {
            // 맵 복귀 시 사후 대사
            if (GameManager.I != null && !string.IsNullOrEmpty(GameManager.I.PendingPostDialogueId))
            {
                string id = GameManager.I.PendingPostDialogueId;
                GameManager.I.ClearPendingPostDialogue();
                DialogueManager.I?.TryPlay(id);
            }
        }

        /// <summary>MapNode가 클릭 시 호출.</summary>
        public void OnNodeSelected(NodeSO node)
        {
            if (node == null || node.stages == null || node.stages.Length == 0) return;
            _pendingNode  = node;
            _pendingStage = node.stages[0];

            bool started = DialogueManager.I != null
                        && DialogueManager.I.TryPlay(_pendingStage.preDialogueId);
            if (started) _waitingForPreDialogue = true;
            else         ShowPopup();
        }

        void OnDialogueEnd()
        {
            if (!_waitingForPreDialogue) return;
            _waitingForPreDialogue = false;
            ShowPopup();
        }

        void ShowPopup()
        {
            if (stagePopup && _pendingNode && _pendingStage)
                stagePopup.Show(_pendingNode, _pendingStage);
        }
    }
}
```

- [ ] **Step 2: MapNode 전체 교체**

`Assets/Scripts/Map/MapNode.cs` 전체를 아래로 교체 (`GameManager.EnterStage` 직접 호출 → `MapFlowController` 위임):

```csharp
using UnityEngine;
using UnityEngine.UI;
using TOME.Data;
using TOME.Managers;

namespace TOME.Map
{
    /// <summary>UGUI Button 기반. 클릭 시 MapFlowController로 위임.</summary>
    [RequireComponent(typeof(Button))]
    public class MapNode : MonoBehaviour
    {
        [SerializeField] NodeSO  def;
        [SerializeField] Image   icon;
        [SerializeField] MapFlowController flow;
        [SerializeField] Color   lockedColor   = new(0.4f, 0.4f, 0.4f, 1f);
        [SerializeField] Color   unlockedColor = Color.white;

        Button _btn;
        bool   _lastUnlocked;

        void Awake()
        {
            _btn = GetComponent<Button>();
            _btn.onClick.AddListener(OnClick);
            if (def && icon && def.iconOnMap) icon.sprite = def.iconOnMap;
        }

        void Start()    { Refresh(true); }
        void OnEnable() { Refresh(true); }

        void Refresh(bool force)
        {
            if (!def) return;
            bool u = MapManager.I != null && MapManager.I.IsUnlocked(def);
            if (!force && u == _lastUnlocked) return;
            _lastUnlocked = u;
            _btn.interactable = u;
            if (icon) icon.color = u ? unlockedColor : lockedColor;
        }

        public void NotifyUnlockChanged() => Refresh(false);

        void OnClick()
        {
            if (!def || MapManager.I == null || !MapManager.I.IsUnlocked(def)) return;
            if (flow) flow.OnNodeSelected(def);
        }
    }
}
```

- [ ] **Step 3: 컴파일 확인**

`refresh_unity` → `read_console` (Error). Expected: 에러 0건. **이 시점에서 모든 스크립트 컴파일이 깨끗해야 한다.**

- [ ] **Step 4: 커밋**

```bash
git add Assets/Scripts/Map/MapFlowController.cs Assets/Scripts/Map/MapNode.cs
git commit -m "맵 흐름 제어 추가, MapNode 위임 구조로 변경

Co-Authored-By: Claude Opus 4.7 <noreply@anthropic.com>"
```

---

## Task 10: ScriptableObject 에셋 생성

의존성 순서대로 생성. 모두 `Assets/Data/` 하위. `mcp__UnityMCP__manage_scriptable_object`로 생성 후 필드 설정.

스프라이트 경로 참조:
- 강아지: `Assets/Sprites/Characters/Dog_nomal.png`
- 투사체: `Assets/Sprites/Game/Avoid/Bullet.png`
- 적: `Assets/Sprites/Game/Avoid/Enemy_Avoid.png`
- 아이템: `Assets/Sprites/Game/Avoid/item_speed.png`, `item_score.png`, `item_shield.png`, `item_time.png`

- [ ] **Step 1: AttackPattern 생성**

`Assets/Data/Attack/Atk_DogBark.asset` (`AttackPatternSO`):
- type = Projectile
- projectilePrefab = (Task 11에서 생성할 `Projectile` 프리팹 — 지금은 비워두고 Task 11 Step 6에서 연결)
- projectileSpeed = 12
- aoeRadius = 0, pierce = 0

- [ ] **Step 2: Item 4종 생성**

`Assets/Data/Items/` 하위:

| 파일 | id | displayName | icon | tier |
|---|---|---|---|---|
| `Item_Potion.asset` | potion | 포션 | item_speed.png | Basic |
| `Item_Star.asset` | star | 별 | item_score.png | Basic |
| `Item_Sword.asset` | sword | 검 | item_shield.png | Rare |
| `Item_Gem.asset` | gem | 보석 | item_time.png | Rare |

- [ ] **Step 3: Enemy 생성**

`Assets/Data/Enemies/Enemy_Ghost.asset` (`EnemySO`):
- id = ghost, sprite = Enemy_Avoid.png, anim = (없음)
- hp = 30, atk = 5, moveSpeed = 1.5
- dropTable = [Item_Potion, Item_Star, Item_Sword, Item_Gem]
- dropWeights = [1, 1, 1, 1]
- dropChance = 0.6

- [ ] **Step 4: Character 4종 생성**

`Assets/Data/Characters/` 하위. 모두 icon = Dog_nomal.png, attackPattern = Atk_DogBark. corePrefab은 Task 11 Step 6에서 연결.

| 파일 | id | displayName | hp | atk | atkSpeed | atkRange | bodyTint (RGBA) |
|---|---|---|---|---|---|---|---|
| `Char_StrayDog.asset` | stray_dog | 사패 강아지 | 100 | 10 | 1.0 | 4 | (1, 1, 1, 1) |
| `Char_Wizard.asset` | wizard | 마법사 사이느라 | 90 | 18 | 0.8 | 5 | (0.7, 0.5, 1, 1) |
| `Char_King.asset` | king | 왕 | 140 | 14 | 1.1 | 4 | (1, 0.85, 0.3, 1) |
| `Char_Angel.asset` | angel | 천사 | 120 | 16 | 1.5 | 5 | (0.6, 0.85, 1, 1) |

- [ ] **Step 5: Recipe 3종 생성**

`Assets/Data/Recipes/` 하위 (`RecipeSO`):

| 파일 | ingredients | result |
|---|---|---|
| `Recipe_Wizard.asset` | [Item_Potion, Item_Star] | Char_Wizard |
| `Recipe_King.asset` | [Item_Sword, Item_Gem, Item_Star] | Char_King |
| `Recipe_Angel.asset` | [Item_Potion, Item_Star, Item_Sword, Item_Gem] | Char_Angel |

- [ ] **Step 6: Reward 생성**

`Assets/Data/Rewards/Reward_Stage1.asset` (`RewardSO`):
- type = Coin, amount = 50, item = (없음), character = (없음)

- [ ] **Step 7: Node 생성**

`Assets/Data/Nodes/Node_Hallway.asset` (`NodeSO`):
- id = hallway, nodeName = 복도
- mapPosition = (0, 0), iconOnMap = `Assets/Sprites/Map_hallway/16_pad.png` (없으면 Dog_nomal.png)
- stages = [Stage_1] (Step 8에서 생성 후 연결)
- bonus = 기본값(전 필드 0)
- unlocksOnClear = [] (빈 배열)
- unlockedByDefault = true

- [ ] **Step 8: Stage 생성**

`Assets/Data/Stages/Stage_1.asset` (`StageSO`):
- id = stage1, title = "집탈출 시도 1"
- thumbnail = Dog_nomal.png
- introText = "택배 상자를 파괴 시켜 주인의 정신을 쏙 빼놓자"
- clearedIntroText = "택배 상자를 파괴 시켜 주인의 정신을 쏙 빼놓자\n>성공했다하하하하"
- startCharacter = Char_StrayDog
- timeLimit = 60
- spawns = [ { enemy: Enemy_Ghost, totalCount: 3, simultaneous: 2, spawnInterval: 2.0, startDelay: 0.5 } ]
- preDialogueId = "intro_stage1_pre"
- postDialogueId = "intro_stage1_post"
- rewards = [Reward_Stage1]

- [ ] **Step 9: 상호 참조 연결**

- `Node_Hallway.stages` = [Stage_1]
- 컴파일/임포트 확인: `refresh_unity` → `read_console` (Error). Expected: 에러 0건.

- [ ] **Step 10: 커밋**

```bash
git add Assets/Data
git commit -m "스테이지1 ScriptableObject 데이터 에셋 생성

Co-Authored-By: Claude Opus 4.7 <noreply@anthropic.com>"
```

---

## Task 11: 프리팹 생성

`mcp__UnityMCP__manage_gameobject` / `manage_prefabs`로 생성. 모두 `Assets/Prefabs/` 하위. 2D URP 환경 (SpriteRenderer, Collider2D).

- [ ] **Step 1: Projectile 프리팹**

`Assets/Prefabs/Items/Projectile.prefab`:
- 루트: SpriteRenderer (sprite = Bullet.png, sortingOrder = 5)
- `Projectile` 컴포넌트
- 스케일 적절히(0.5 정도)

- [ ] **Step 2: Enemy 프리팹**

`Assets/Prefabs/Enemies/Enemy.prefab`:
- 루트: SpriteRenderer (sortingOrder = 3), `EnemyBase` 컴포넌트
- CircleCollider2D (isTrigger = false, radius ≈ 0.5)
- Rigidbody2D (bodyType = Kinematic, gravityScale = 0)
- 태그/레이어 기본

- [ ] **Step 3: CharacterCore 프리팹 4종**

`Assets/Prefabs/Player/` 하위. 각 프리팹은 SpriteRenderer(sprite = Dog_nomal.png, sortingOrder = 6) + `CharacterCore` 컴포넌트. `CharacterCore.body`에 SpriteRenderer 연결. 변형은 스케일만 다름:

| 프리팹 | localScale |
|---|---|
| `CharacterCore_StrayDog.prefab` | (1, 1, 1) |
| `CharacterCore_Wizard.prefab` | (0.95, 0.95, 1) |
| `CharacterCore_King.prefab` | (1.15, 1.15, 1) |
| `CharacterCore_Angel.prefab` | (1.05, 1.05, 1) |

- [ ] **Step 4: Player 프리팹**

`Assets/Prefabs/Player/Player.prefab`:
- 루트: `PlayerShell` 컴포넌트 + `AutoAttack` 컴포넌트
- CircleCollider2D (isTrigger = true, radius ≈ 0.5)
- Rigidbody2D (bodyType = Kinematic, gravityScale = 0)
- 자식 빈 GameObject `CorePivot` (localPosition 0)
- `PlayerShell` 필드: corePivot = CorePivot, autoAttack = AutoAttack 컴포넌트, dragYMin = -8, dragYMax = -2, dragLerpSpeed = 25
- `AutoAttack`는 SerializeField 없음 (런타임 Configure)

- [ ] **Step 5: ItemPickup 프리팹**

`Assets/Prefabs/Items/ItemPickup.prefab`:
- 루트: SpriteRenderer (sortingOrder = 4) + `ItemPickup` 컴포넌트
- CircleCollider2D (isTrigger = true, radius ≈ 0.4)
- (Rigidbody2D 불필요 — Player가 Rigidbody2D 보유)
- 스케일 0.6 정도

- [ ] **Step 6: SO ↔ 프리팹 상호 참조 연결**

- `Atk_DogBark.projectilePrefab` = `Projectile.prefab`
- `Char_StrayDog.corePrefab` = `CharacterCore_StrayDog.prefab`
- `Char_Wizard.corePrefab` = `CharacterCore_Wizard.prefab`
- `Char_King.corePrefab` = `CharacterCore_King.prefab`
- `Char_Angel.corePrefab` = `CharacterCore_Angel.prefab`

- [ ] **Step 7: 임포트 확인 및 커밋**

`refresh_unity` → `read_console` (Error). Expected: 에러 0건.

```bash
git add Assets/Prefabs Assets/Data
git commit -m "스테이지1 프리팹 생성 및 데이터 연결

Co-Authored-By: Claude Opus 4.7 <noreply@anthropic.com>"
```

---

## Task 12: Boot 씬

DontDestroyOnLoad 매니저 + 페이더.

**Files:**
- Create: `Assets/Scenes/Boot.unity`

- [ ] **Step 1: Boot 씬 생성**

`mcp__UnityMCP__manage_scene`로 `Assets/Scenes/Boot.unity` 생성. Main Camera + Directional Light 포함.

- [ ] **Step 2: 매니저 GameObject 배치**

빈 GameObject들 생성 후 컴포넌트 부착:
- `GameManager` ← `GameManager`
- `DialogueManager` ← `DialogueManager`. 필드 `dialogueCsv` = `Assets/CSV/dialogue.csv`
- `SaveSystemManager` ← `SaveSystemManager`
- `MapManager` ← `MapManager`. 필드 `allNodes` = [`Node_Hallway`]
- `BootstrapEntry` ← `BootstrapEntry`. 필드 `firstScene` = "Map" (기본값 확인)

- [ ] **Step 3: SceneFader 구성**

- `SceneFaderCanvas` GameObject: Canvas (renderMode = ScreenSpaceOverlay, sortingOrder = 999) + CanvasScaler (1080×1920 기준) + GraphicRaycaster
- 자식 `FadeImage`: Image (검정, 전체 화면 stretch) + CanvasGroup (alpha = 0)
- `SceneFaderCanvas`에 `SceneFader` 컴포넌트, 필드 `group` = FadeImage의 CanvasGroup

- [ ] **Step 4: 씬 저장 및 검증**

`manage_scene` save. Play 모드 진입 → `read_console` 확인: 에러 없이 Map 씬으로 전환 시도(아직 Map 씬 없으면 "Scene not in build settings" 경고 가능 — Task 15에서 해결). Play 정지.

- [ ] **Step 5: 커밋**

```bash
git add Assets/Scenes/Boot.unity Assets/Scenes/Boot.unity.meta
git commit -m "Boot 씬 추가 (전역 매니저, 페이더)

Co-Authored-By: Claude Opus 4.7 <noreply@anthropic.com>"
```

---

## Task 13: Map 씬

복도 배경 + 노드 1개 + 대사/팝업 UI.

**Files:**
- Create: `Assets/Scenes/Map.unity`

- [ ] **Step 1: Map 씬 생성**

`Assets/Scenes/Map.unity` 생성. Main Camera (Orthographic, size = 10, position = (0,0,-10), background 적절히) + Directional Light.

- [ ] **Step 2: 배경 구성**

- `Background` SpriteRenderer: sprite = `Assets/Sprites/Map_hallway/0_entire_background.png`, sortingOrder = 0
- `Foreground` SpriteRenderer: sprite = `Assets/Sprites/Map_hallway/0_entire.png`, sortingOrder = 1
- 카메라 뷰에 맞게 스케일 조정 (1080×1920 세로 화면 채우기)

- [ ] **Step 3: 매니저 + 흐름 컨트롤러**

- `MapFlowController` GameObject ← `MapFlowController` 컴포넌트

- [ ] **Step 4: UI 캔버스 구성**

`MapCanvas`: Canvas (ScreenSpaceOverlay) + CanvasScaler (referenceResolution = 1080×1920, matchWidthOrHeight = 0.5) + GraphicRaycaster. EventSystem GameObject도 생성.

자식 구성:
- **노드 버튼** `NodeButton`: Button + Image (sprite = 16_pad.png 또는 노드 아이콘), 화면 중앙 하단쯤 배치. `MapNode` 컴포넌트 부착, 필드: def = `Node_Hallway`, icon = 자신의 Image, flow = `MapFlowController`
- **대사 UI** `DialogueUI`: `DialogueUI` 컴포넌트. 자식 `root`(Panel) 안에 화자 라벨(TMP), 본문 라벨(TMP), 스킵 버튼. `root`는 전체 화면을 덮는 투명 패널 + 하단 말풍선. `DialogueUI` 필드: root, speakerLabel, textLabel, skipButton 연결. root의 Image에 Raycast Target 켜서 클릭 진행 동작.
- **스테이지 정보 팝업** `StageInfoPopup`: `StageInfoPopupUI` 컴포넌트. 자식 `root`(Panel) 안에 thumbnail(Image), characterNameLabel(TMP, 볼드), introLabel(TMP), clearBadge(GameObject, "CLEAR" TMP), startButton(Button, "START"), closeButton(Button, "X"). `StageInfoPopupUI` 필드 전부 연결.

- [ ] **Step 5: 씬 저장 및 검증**

저장 후 `read_console` 확인. 에러 0건.

- [ ] **Step 6: 커밋**

```bash
git add Assets/Scenes/Map.unity Assets/Scenes/Map.unity.meta
git commit -m "Map 씬 추가 (복도 배경, 노드, 대사/팝업 UI)

Co-Authored-By: Claude Opus 4.7 <noreply@anthropic.com>"
```

---

## Task 14: Stage 씬

전투 + UI + 결과창.

**Files:**
- Create: `Assets/Scenes/Stage.unity`

- [ ] **Step 1: Stage 씬 생성**

`Assets/Scenes/Stage.unity` 생성. Main Camera (Orthographic, size = 10, position = (0,0,-10), 배경색 연한 핑크 #F8C8C8 정도) + Directional Light.

- [ ] **Step 2: 전투 루트 오브젝트**

- `EnemyRoot` 빈 GameObject (적 풀 부모)
- `ItemRoot` 빈 GameObject (아이템 풀 부모)
- `PlayerSpawn` 빈 GameObject, position = (0, -5, 0)
- `Player`: `Player.prefab` 인스턴스 배치, position = (0, -5, 0)

- [ ] **Step 3: 매니저 배치**

- `CombatManager` ← `CombatManager`. 필드: enemyRoot = EnemyRoot, enemyPrefab = `Enemy.prefab`, spawnXRange = (-3, 3), spawnY = 4.5, prewarmPerType = 4
- `InventoryManager` ← `InventoryManager`
- `MergeCraftManager` ← `MergeCraftManager`
- `ItemDropManager` ← `ItemDropManager`. 필드: itemPickupPrefab = `ItemPickup.prefab`, itemRoot = ItemRoot, collectionY = -7, driftSpeed = 2.5, timedDropInterval = 6, timedSpawnXRange = (-2.5, 2.5), timedSpawnY = 3, prewarm = 6
- `StageManager` ← `StageManager`. 필드: player = Player, playerSpawn = PlayerSpawn, recipes = [Recipe_Wizard, Recipe_King, Recipe_Angel], itemDropManager = ItemDropManager, resultScreen = (Step 5에서 연결)

- [ ] **Step 4: HUD 캔버스**

`StageCanvas`: Canvas (ScreenSpaceOverlay) + CanvasScaler (1080×1920, match 0.5) + GraphicRaycaster. EventSystem 생성.

- **상단 HUD** `Hud`: `HudUI` 컴포넌트. 자식:
  - countLabel (TMP, 우상단 "남은적 0/3")
  - timerLabel (TMP, 우상단 아래 "60s")
  - hpBar (Slider, 상단 HP 바)
  - `HudUI` 필드: countLabel, timerLabel, hpBar, player = Player, craftPanel = (Step 4 CraftPanel)
- **하단 인벤토리 버튼** `InventoryButton`: Button (하단 바, "인벤토리" 라벨). OnClick → `Hud`의 `HudUI.OnClickInventory`

- [ ] **Step 5: 조합 패널**

`CraftPanel`: `CraftPanelUI` 컴포넌트. 자식 `root`(Panel, 게임 화면을 반투명하게 덮는 패널 — 상단 전투 영역 가리지 않도록 하단 절반에 배치, 1920 기준 세로 960):
- `InventoryBar` 영역: `InventoryBarUI` 컴포넌트. 자식: slotContainer (Horizontal Layout Group), prevButton(`<`), nextButton(`>`). slotButtonPrefab = `InventorySlotButton` 프리팹 (아래 Step 5a)
- 4개 조합 슬롯 `CraftSlot0~3`: 각각 Button + Image + `CraftSlotButton` 컴포넌트. CraftSlotButton.icon = 자식 Image
- `ResultButton`: Button + 자식 resultIcon(Image), resultLabel(TMP)
- `CloseButton`: Button ("X")
- `CraftPanelUI` 필드 전부 연결: root, inventoryBar, craftSlots = [CraftSlot0..3], resultButton, resultIcon, resultLabel, closeButton, hud = Hud

- [ ] **Step 5a: InventorySlotButton 프리팹**

`Assets/Prefabs/UI/InventorySlotButton.prefab`: Button + 자식 Image(icon) + `InventorySlotButton` 컴포넌트 (icon 필드 연결). InventoryBarUI.slotButtonPrefab에 연결.

- [ ] **Step 6: 결과창**

`ResultScreen`: `ResultScreenUI` 컴포넌트. 자식 `root`(Panel, 전체 화면):
- clearGraphic (GameObject, "CLEAR" + 별 이미지군)
- failGraphic (GameObject, "FAIL" 텍스트)
- starsGroup (GameObject, 별 5개)
- rewardContainer (Transform, Horizontal Layout)
- returnButton (Button, "돌아가기")
- `centerAnchor` 빈 GameObject, position = (0, 0, 0)
- `ResultScreenUI` 필드: root, clearGraphic, failGraphic, starsGroup, rewardContainer, rewardIconPrefab = `RewardIcon` 프리팹(Step 6a), returnButton, player = Player, centerAnchor, moveDuration = 0.5, showDelay = 2
- `StageManager.resultScreen` = ResultScreen 연결

- [ ] **Step 6a: RewardIcon 프리팹**

`Assets/Prefabs/UI/RewardIcon.prefab`: 자식 Image + TMP_Text. ResultScreenUI.rewardIconPrefab에 연결.

- [ ] **Step 7: 씬 저장 및 검증**

저장 후 `read_console` 확인. 에러 0건.

- [ ] **Step 8: 커밋**

```bash
git add Assets/Scenes/Stage.unity Assets/Scenes/Stage.unity.meta Assets/Prefabs/UI
git commit -m "Stage 씬 추가 (전투, HUD, 조합 패널, 결과창)

Co-Authored-By: Claude Opus 4.7 <noreply@anthropic.com>"
```

---

## Task 15: Build Settings 등록 및 전체 검증

**Files:**
- Modify: `ProjectSettings/EditorBuildSettings.asset`

- [ ] **Step 1: Build Settings에 씬 등록**

`mcp__UnityMCP__manage_editor` 또는 `manage_build`로 빌드 씬 목록 설정 (순서 중요):
1. `Assets/Scenes/Boot.unity` (index 0)
2. `Assets/Scenes/Map.unity` (index 1)
3. `Assets/Scenes/Stage.unity` (index 2)

`SampleScene.unity`는 목록에서 제외.

- [ ] **Step 2: EditMode 테스트 재실행**

`mcp__UnityMCP__run_tests` (EditMode). Expected: RecipeMatcherTests 2개 PASS.

- [ ] **Step 3: 전체 루프 Play 검증**

Boot 씬을 열고 Play 모드 진입. `read_console`로 에러 모니터링하며 다음을 확인:
1. Boot → Map 씬 자동 전환 (페이드)
2. 노드 클릭 → 사전 대사 "이런 젠장~" → "가출해 주겟슨~" → (탭으로 진행)
3. 대사 종료 → 스테이지 정보 팝업 표시 (썸네일/소개문/사패 강아지/START)
4. START → Stage 씬 전환
5. 적(유령) 스폰, 플레이어 자동 공격(투사체), 적 추적
6. 적 처치 / 시간 경과 시 아이템 드랍 → 수집 영역으로 이동 → 플레이어 충돌 시 인벤토리 획득
7. 인벤토리 버튼 → 게임 일시정지 + 조합 패널 표시
8. 인벤토리 아이템 클릭 → 조합 슬롯 배치, 조합 슬롯 클릭 → 인벤토리 회수
9. 포션+별 배치 → 결과창에 "마법사 사이느라" 미리보기 → 결과 버튼 클릭 → 캐릭터 교체 + 전투 재개
10. 전멸 시 승리 / HP 0 또는 시간초과 시 패배 → 결과창: 캐릭터 중앙 이동, CLEAR/FAIL, (승리 시) 보상, 2초 후 돌아가기 버튼
11. 돌아가기 → Map 씬 복귀 → (승리했다면) 사후 대사 "집 비밀번호..." 재생
12. 다시 노드 클릭 → 사전 대사 생략(이미 봄) → 팝업이 CLEAR 배지 표시

각 단계에서 콘솔 에러가 나면 해당 스크립트/와이어링 수정 후 재검증. (UI는 시각 확인이 필요하므로, 자동 검증이 불가능한 부분은 사용자에게 Play 테스트를 요청)

- [ ] **Step 4: 최종 커밋**

```bash
git add ProjectSettings/EditorBuildSettings.asset
git commit -m "빌드 씬 등록 (Boot/Map/Stage) 및 전체 루프 검증

Co-Authored-By: Claude Opus 4.7 <noreply@anthropic.com>"
```

---

## Self-Review 결과

**Spec coverage:**
- 흐름(맵→대사→팝업→전투→조합→결과→복귀→사후대사) → Task 12~15 ✓
- 씬 3개 → Task 12/13/14 ✓
- 신규 스크립트 8개 → Task 5/6/7/8/9 ✓ (+ 리프 스크립트 InventorySlotButton/CraftSlotButton)
- 기존 스크립트 수정 → Task 3/4/8/9 ✓
- SO 에셋 → Task 10 ✓
- 프리팹 → Task 11 ✓
- 조합 능력치/레시피 → Task 10 Step 4/5 ✓
- 플레이스홀더 아트(bodyTint) → Task 3/10/11 ✓
- 비기능(풀링/구독해제/timeScale/null가드) → 각 스크립트에 반영 ✓
- 테스트 → Task 2 ✓

**Placeholder scan:** "TBD/TODO" 없음. 모든 스크립트는 완전한 코드. MCP 에디터 작업은 정확한 필드값 표로 명시.

**Type consistency:** `OnEnemyKilled(EnemySO,Vector3)`, `CraftPanelUI.Open()`, `HudUI.OnClickInventory/OnClickCloseCraft`, `MapFlowController.OnNodeSelected(NodeSO)`, `ResultScreenUI.Show(bool)`, `ItemDropManager.Begin(EnemySO)/Stop()`, `GameManager.RecordStageResult/ClearPendingPostDialogue` — Task 간 시그니처 일치 확인 완료.

**의존성 주의:** Task 4 Step 5(StageManager)는 Task 5·8 완료 후 적용 (Step 6b). 이는 Task 4/8 본문에 명시됨.
