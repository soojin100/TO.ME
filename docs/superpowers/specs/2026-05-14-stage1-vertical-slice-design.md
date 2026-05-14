# 스테이지1 수직 슬라이스 — 설계 문서

작성일: 2026-05-14
기반 기획서: `NewGame_TO.ME` (오토배틀 + 머지, Unity 6000.4.0f1, 1080×1920 세로)

## 목표

기존 코드 뼈대(매니저·ScriptableObject 정의·풀링·세이브·씬 로더) 위에 실제로 플레이 가능한 스테이지 1개를 완성한다. 맵에서 스테이지를 누르고, 전투하고, 아이템을 조합해 캐릭터를 교체하고, 승패 결과를 보고, 맵으로 돌아오는 전체 루프를 검증하는 것이 범위다.

작업 방식: Unity Editor를 MCP로 직접 조작하여 씬·프리팹·ScriptableObject 에셋을 생성하고 컴포넌트를 와이어링한다.

## 비범위 (이번 슬라이스 제외)

- 다중 스테이지 / 다중 노드 맵
- Title / Ending 씬
- 조합 레시피 11종 전부 (3종만)
- 스타트 캐릭터 커스터마이징 시스템
- 세이브 데이터 암호화 (추후 과제)
- 실제 조합 캐릭터 아트 (플레이스홀더로 대체)

## 전체 흐름 (end-to-end)

```
Boot 씬
  └→ Map 씬 (복도 배경, 노드 1개)
       └→ 노드(호출기) 탭
            └→ 사전 대사 재생 (한 번만, intro_stage1_pre)
                 └→ 스테이지 정보 팝업 (썸네일/제목/소개문/START)
                      └→ START → Stage 씬
                           ├→ 시작 캐릭터 장착 (사패 강아지)
                           ├→ 오토배틀: 적 스폰, 자동 공격, 추적
                           ├→ 아이템 드랍 (적 사망 / 시간 경과)
                           ├→ 플레이어 드래그 이동, 아이템 충돌 시 인벤토리 획득
                           ├→ 인벤토리 클릭 → 일시정지 → 조합 패널
                           │    └→ 4슬롯 배치 → 결과 미리보기 → 결과창 클릭
                           │         └→ 조합 성공 → 캐릭터 교체 → 재개
                           └→ 종료 (전멸=승리 / HP 0 또는 시간초과=패배)
                                └→ 결과창 (캐릭터 중앙 이동, CLEAR/FAIL, 별, 보상)
                                     └→ 2초 후 돌아가기 버튼 → Map 씬
                                          └→ (승리 시) 사후 대사 재생 (intro_stage1_post)
                                               └→ 스테이지 정보 팝업이 CLEAR 상태로 갱신
```

## 씬 구성 (3개)

### Boot 씬
- `BootstrapEntry` + DontDestroyOnLoad 매니저 전부를 미리 배치:
  `DialogueManager`, `SaveSystemManager`, `GameManager`, `MapManager`, `SceneFader`
- 카메라 + Directional Light 포함
- CSV 대사 1회 프리로드 후 Map 씬 비동기 로드

### Map 씬
- 복도 배경: `Sprites/Map_hallway` 스프라이트 사용
- `MapNode` 1개: 호출기 디바이스 위치, `Node_Hallway` 참조
- `MapFlowController`: 노드 탭 핸들링 + 맵 복귀 시 사후 대사 트리거
- 대사 UI 캔버스 (`DialogueUI`)
- 스테이지 정보 팝업 캔버스 (`StageInfoPopupUI`)
- 카메라 + Light

### Stage 씬
- `StageManager` (라이프사이클)
- `PlayerShell` + `corePivot` + `AutoAttack` + Collider2D
- `CombatManager`: `enemyRoot`, `enemyPrefab`, 스폰 파라미터
- `ItemDropManager`: 적 사망/시간 드랍
- `HudUI`: 상단 적 카운트·타이머·HP, 하단 인벤토리 진입 버튼
- `CraftPanelUI` + `InventoryBarUI` 캔버스
- `ResultScreenUI` 캔버스
- 카메라 + Light

## 신규 스크립트

기존 뼈대에 빠진 부분을 채운다. 네임스페이스 규칙은 기존과 동일(`TOME.*`).

| 스크립트 | 네임스페이스 | 책임 |
|---|---|---|
| `ItemDropManager` | `TOME.Managers` | 적 사망 콜백 + 주기 타이머로 필드에 `ItemPickup` 스폰 (풀링). 드랍 테이블/가중치 기반 아이템 선택 |
| `ItemPickup` | `TOME.Gameplay.Items` | 필드 아이템. 스폰 후 "아이템 여기로 옴" 영역으로 드리프트, 플레이어 충돌 시 `InventoryManager.Add` 후 풀 반환 |
| `DialogueUI` | `TOME.UI` | `DialogueManager.OnLine/OnEnd` 구독, 말풍선+화자 표시, 탭으로 `Advance()`, 스킵 버튼 |
| `StageInfoPopupUI` | `TOME.UI` | 썸네일/제목/소개문/START 버튼. 클리어 노드면 CLEAR 라벨 + 변경된 소개문 표시 |
| `InventoryBarUI` | `TOME.UI` | `InventoryManager.Items` 바인딩, 가로 스크롤(`<`/`>`), 아이콘 30% 축소 표시, 슬롯 클릭 시 조합창으로 |
| `CraftPanelUI` | `TOME.UI` | 4슬롯 + 결과창. `MergeCraftManager` 바인딩, 슬롯 배치/회수, 미리보기, 결과창 클릭→`Craft()`, 조합 연출 |
| `ResultScreenUI` | `TOME.UI` | 캐릭터 중앙 이동, CLEAR/FAIL 스프라이트, 별, 보상 아이콘, 2초 지연 후 돌아가기 버튼 |
| `MapFlowController` | `TOME.Map` | 노드 탭 → 사전 대사(있으면)→종료 콜백→팝업 / 이미 본 대사면 팝업 직행. 맵 진입 시 `PendingPostDialogueId` 있으면 사후 대사 재생 |

## 기존 스크립트 수정 (최소)

- **`StageManager`**: `DialogueManager.TryPlay(preDialogueId)` / `TryPlay(postDialogueId)` 호출 제거. 대사는 Map 씬으로 이전. 종료 시 `GameManager`에 결과를 기록한다.
- **`GameManager`**: `LastStageResult` (enum: None/Win/Lose) 와 `PendingPostDialogueId` (string) 프로퍼티 추가. `EnterStage` 시 초기화, Stage 종료 시 `StageManager`가 기록, `ReturnToMap` 후 `MapFlowController`가 소비.
- **`CharacterSO`**: `Color bodyTint = Color.white` 필드 1개 추가. `CharacterCore.Bind`에서 `body.color = def.bodyTint` 적용. 플레이스홀더 캐릭터 시각 구분용. (실제 아트 도입 시 tint를 white로 두면 무영향)
- **`CharacterCore`**: `Bind`에 `body.color = def.bodyTint` 한 줄 추가.

## 데이터 에셋 (ScriptableObject)

생성 위치: `Assets/Data/` 하위 폴더별 분류.

### AttackPattern
- `Atk_DogBark`: type=Projectile, projectilePrefab=Projectile 프리팹, projectileSpeed=12

### Character (4종)
| 에셋 | id | displayName | HP | ATK | 공속 | 사거리 | bodyTint | 컨셉 |
|---|---|---|---|---|---|---|---|---|
| `Char_StrayDog` | stray_dog | 사패 강아지 | 100 | 10 | 1.0 | 4 | white | 시작 캐릭터 |
| `Char_Wizard` | wizard | 마법사 사이느라 | 90 | 18 | 0.8 | 5 | (보라 계열) | 유리대포 (2조합) |
| `Char_King` | king | 왕 | 140 | 14 | 1.1 | 4 | (금색 계열) | 탱커 (3조합) |
| `Char_Angel` | angel | 천사 | 120 | 16 | 1.5 | 5 | (하늘색 계열) | 만능·최상위 (4조합) |

- 전 캐릭터 `attackPattern = Atk_DogBark`, `corePrefab` = 각자의 CharacterCore 프리팹 변형
- `icon` = `Dog_nomal.png` (플레이스홀더 공통), 시각 구분은 `bodyTint` + 코어 프리팹 스케일

### Enemy
- `Enemy_Ghost`: id=ghost, sprite=`Enemy_Avoid.png`(임시), hp=30, atk=5, moveSpeed=1.5, dropTable=[Potion, Star, Sword, Gem], dropWeights=[1,1,1,1], dropChance=0.6

### Item (4종)
| 에셋 | id | displayName | tier | icon (임시) |
|---|---|---|---|---|
| `Item_Potion` | potion | 포션 | Basic | `item_speed.png` |
| `Item_Star` | star | 별 | Basic | `item_score.png` |
| `Item_Sword` | sword | 검 | Rare | `item_shield.png` |
| `Item_Gem` | gem | 보석 | Rare | `item_time.png` |

### Recipe (3종) — 순서 무관
| 에셋 | 재료 | 결과 |
|---|---|---|
| `Recipe_Wizard` | Potion + Star | Char_Wizard |
| `Recipe_King` | Sword + Gem + Star | Char_King |
| `Recipe_Angel` | Potion + Star + Sword + Gem | Char_Angel |

### Reward
- `Reward_Stage1`: type=Coin, amount=50

### Node / Stage
- `Node_Hallway`: id=hallway, nodeName=복도, unlockedByDefault=true, stages=[Stage_1], bonus=기본(0), unlocksOnClear=[] (슬라이스에선 비움)
- `Stage_1`: id=stage1, title="집탈출 시도 1", startCharacter=Char_StrayDog, timeLimit=60, spawns=[{enemy:Ghost, totalCount:3, simultaneous:2, spawnInterval:2.0, startDelay:0.5}], preDialogueId=intro_stage1_pre, postDialogueId=intro_stage1_post, rewards=[Reward_Stage1]

## 프리팹

풀링·인스턴스 대상만 프리팹화. UI는 단일 사용이라 씬 캔버스에 직접 구성.

| 프리팹 | 구성 |
|---|---|
| `Player` | PlayerShell + corePivot(빈 Transform) + AutoAttack + CircleCollider2D(isTrigger) + Rigidbody2D(kinematic) |
| `CharacterCore_*` (4종) | SpriteRenderer + CharacterCore. 각 변형은 스케일만 다름 (색은 SO의 bodyTint) |
| `Enemy` | EnemyBase + SpriteRenderer + CircleCollider2D + Rigidbody2D(kinematic) |
| `Projectile` | Projectile + SpriteRenderer (충돌은 거리 기반, 콜라이더 불필요) |
| `ItemPickup` | ItemPickup + SpriteRenderer + CircleCollider2D(isTrigger) |

## 데이터 흐름

### Map → Stage
1. `MapNode` 클릭 → `MapFlowController.OnNodeSelected(node)`
2. `DialogueManager.TryPlay(stage.preDialogueId)` — true면 `OnEnd` 구독 후 종료 시 팝업, false(이미 봄)면 즉시 팝업
3. `StageInfoPopupUI.Show(node, stage)` — 클리어 여부에 따라 라벨 분기
4. START 버튼 → `GameManager.EnterStage(node, stage)` → Stage 씬 로드

### Stage 내부
1. `StageManager.Start`: `RecipeMatcher.Init(recipes)`, `InventoryManager.Clear()`, `player.EquipCharacter(startCharacter, node.bonus)`, `CombatManager.BeginStage(stage)`, `ItemDropManager` 활성화
2. `CombatManager`: `SpawnLoop`로 적 스폰, `EnemyBase`가 플레이어 추적·접촉 데미지
3. `AutoAttack`: 사거리 내 최근접 적에게 투사체 발사
4. `EnemyBase.Die` → `CombatManager.OnEnemyDied` → `ItemDropManager`가 드랍 판정 → `ItemPickup` 스폰
5. `ItemPickup`이 수집 영역으로 이동, 플레이어 트리거 충돌 → `InventoryManager.Add`
6. `HudUI.OnClickInventory` → `CombatManager.Pause()` + `CraftPanelUI` 표시
7. `CraftPanelUI`: 슬롯 배치 → `MergeCraftManager.PlaceFromInventory` → `Preview()` → 결과창 클릭 → `Craft()` → `OnCraftSucceeded` → `StageManager.OnCrafted` → `player.EquipCharacter` + `CombatManager.Resume()`
8. `CombatManager.Finish(win)` → `OnFinished` → `StageManager.OnFinished`: 승리 시 `MapManager.MarkNodeCleared`, `GameManager`에 결과+사후대사ID 기록 → `ResultScreenUI.Show(win)`
9. `ResultScreenUI`: 캐릭터 중앙 이동 연출, CLEAR/FAIL + 별 + 보상, 2초 후 돌아가기 버튼 → `GameManager.ReturnToMap()`

### Stage → Map (복귀)
1. Map 씬 로드, `MapFlowController.Start`
2. `GameManager.PendingPostDialogueId` 있으면 `DialogueManager.TryPlay(post)` (승리 시에만 세팅됨)
3. 사후 대사 종료 후 `StageInfoPopupUI`는 노드 클릭 시 CLEAR 상태로 표시

## 비기능 요건

### 최적화 / 메모리
- 적·투사체·아이템 픽업 전부 `ObjectPool` 사용 (기존 패턴)
- 매 프레임 `Find*` 호출 금지 — 참조 캐싱 (기존 `EnemyRegistry`, `_cachedPlayer` 패턴 유지)
- `CharacterCore`는 `PlayerShell._coreCache`로 캐릭터당 1회만 Instantiate
- 이벤트 구독은 `OnDestroy`/`OnDisable`에서 반드시 해제 (기존 `StageManager` 패턴)
- 조합 미리보기는 `_previewBuf` 재사용으로 GC 회피 (기존)

### 안정성 / 오류 처리
- 모든 매니저 접근은 null 가드 (`?.` / `if` 체크) — 기존 패턴
- CSV 파싱은 `CsvImporter`의 따옴표·줄바꿈 안전 처리 유지
- `RecipeMatcher`는 정렬 키 기반 O(1) 매칭, 미매칭 시 null 반환 → 결과창 비활성
- 씬 전환은 `SceneFader` 페이드로 가시적 끊김 방지

### 보안
- 세이브는 `Application.persistentDataPath`의 평문 JSON. 로컬 단일 플레이어·비경쟁 게임이므로 슬라이스 단계에선 암호화 미적용. **추후 과제**: 출시 전 체크섬 또는 난독화 검토.
- 외부 입력은 CSV(개발자 제공 에셋)뿐 — 신뢰 경계 외부 입력 없음.

## 테스트

- **수동 (Play 모드)**: Boot→Map→대사→팝업→Stage→전투→드랍→조합→교체→승리→결과창→돌아가기→맵→사후대사 전체 루프. 패배 경로(HP 0, 시간초과)도 확인.
- **EditMode 단위 테스트 1개**: `RecipeMatcher` — 재료 순서를 바꿔도 동일 레시피가 매칭되는지, 미등록 조합은 null인지 검증.

## 작업 순서 (개략)

1. 기존 스크립트 수정 (`GameManager`, `StageManager`, `CharacterSO`, `CharacterCore`)
2. 신규 스크립트 작성 (8개) — 컴파일 통과 확인
3. ScriptableObject 에셋 생성 (AttackPattern → Item → Enemy → Character → Recipe → Reward → Node → Stage 순, 의존성 순서)
4. 프리팹 생성 (Projectile → Enemy → CharacterCore 4종 → Player → ItemPickup)
5. Boot 씬 구성
6. Map 씬 구성 + 와이어링
7. Stage 씬 구성 + 와이어링
8. Build Settings에 3개 씬 등록
9. Play 모드 전체 루프 검증 + RecipeMatcher 테스트
