# 스크립트 구조 규칙

## 레이어와 의존 방향

```
Core  →  Data  →  Gameplay / Systems  →  Map / UI
(위 방향으로만 참조한다. 아래로 내려가는 참조는 금지)
```

| 폴더 | 네임스페이스 | 책임 | 참조해도 되는 것 |
|---|---|---|---|
| `Core/` | `TOME.Core` | 게임 도메인을 모르는 엔진 유틸(세이브·페이드·풀링·상수) | (없음) |
| `Data/` | `TOME.Data` | ScriptableObject·순수 데이터·밸런스 수식 | Core |
| `Gameplay/` | `TOME.Gameplay.<모듈>` | 스테이지 씬의 **액터**(플레이어·적·투사체·픽업) | Core, Data, Systems |
| `Systems/` | `TOME.Systems` | 씬을 가로지르는 **규칙·서비스**(진행도·인벤·조합·대사·전투 오케스트레이션) | Core, Data, Gameplay |
| `Map/` | `TOME.Map` | 맵 씬의 화면 구성·노드·컷신·상호작용 | 전부 |
| `UI/` | `TOME.UI` | 화면 표시 전용 컴포넌트 | 전부 |

### 예외: 씬 컴포지션 루트

`Systems/Stage/StageManager` 는 규칙 계층이 아니라 **Stage 씬의 컴포지션 루트**다.
컴포지션 루트가 자기 씬의 UI(`ResultScreenUI`)를 조립하는 것은 허용한다.
이걸 이벤트로 빼면 씬 와이어링만 늘고 얻는 게 없다.

## 명명 규칙

- **네임스페이스는 위 표의 모듈 단위까지만** 내려간다. 그 아래 폴더(`Systems/Crafting`, `UI/Hud` 등)는
  파일 분류용이며 네임스페이스를 늘리지 않는다. → `using` 이 짧게 유지된다.
- 폴더 이름은 **역할**로 짓는다. `Managers/`, `Utils/`, `Effects/`, `Misc/` 처럼
  타입 접미사나 잡동사니 이름으로 폴더를 만들지 않는다.
- 클래스 이름은 역할이 한눈에 보이게 한다. (`CsvImporter` ✗ → `DialogueCsvImporter` ✓,
  `MapManager` ✗ → `MapProgressionManager` ✓)

## 파일 규칙

- 파일 1개 = 타입 1개. (`SaveData` 는 `SaveSystemManager` 에서 분리)
- 모든 `.cs` 는 **UTF-8** 로 저장한다. CP949 로 저장하면 Unity 가 문자열 리터럴을 깨진 글자로 읽는다.
- 스크립트를 옮길 때는 반드시 `.cs` 와 `.cs.meta` 를 **함께** 옮긴다.
  meta 의 GUID 가 씬·프리팹의 컴포넌트 연결을 유지하는 유일한 키다.

## 에셋 로딩

`Resources.Load` 를 쓰지 않는다. `Resources/` 폴더는 참조 여부와 무관하게 빌드에
**통째로** 포함되어 모바일 초기 로딩 시간과 패키지 용량을 직접 늘린다.
에셋은 `[SerializeField]` 인스펙터 연결로 주입한다.

## 화면 배치 스펙 (구 GameConstants.cs 에서 이관)

아래는 기획 수치 기록용이다. **코드의 진실이 아니다** — 실제 값은 카메라/캔버스 설정과
각 컴포넌트의 `[SerializeField]`(`CombatManager.spawnY`, `ItemDropManager.collectionY`,
`PlayerShell.dragYMin/dragYMax` 등)에 있다.

- 기준 해상도: 세로 540x960 (구 문서의 1080x1920 은 폐기된 값)
- 적 등장 구역 / 드래그 구역 / 인벤토리 바의 세로 분할
- 인벤토리 슬롯 4칸, 아이템 최대 티어 5

## 폐기 기록

`_deprecated/` 는 Unity 가 컴파일하지 않는 격리 폴더다. 컴파일 확인 후 삭제한다.

| 파일 | 폐기 사유 |
|---|---|
| `StageGameManager` `EnemyController` `EnemySpawner` `Bullet` `PlayerController` `DropItem` | 서로만 참조하는 폐쇄 섬. `CombatManager`/`EnemyBase`/`Projectile`/`PlayerShell` 이 대체 |
| `GameConstants` | 사용처 0 + 값이 현 스펙과 불일치. 수치는 위 절로 이관 |
| `MapNode` | 씬 배치 0. `StageNodeButton` 이 대체 |
| `Room_Bedroom` `Room_Hallway` `Room_Kitchen` 씬 | `Map_*` 씬이 대체본. 빌드 세팅에서도 제외되어 있었음 |
| `Making` 씬 | GameObject 4개·스크립트 0개의 빈 실험 씬. 참조·빌드 포함 모두 0 |
| `MapSceneFeatureMigrator` | `Room_*` 씬을 소스로 읽어 `Map_*` 로 기능을 옮기던 **일회성 이관 도구**. 이관이 끝나고 소스 씬이 폐기되어 실행 불가 |

### 아직 씬에 남아 정리가 필요한 것

- **`StageInfoPopupUI`** — `Show()` 호출부 0개라 런타임 도달 불가.
  대체재 `TutorialIntroController`("싸우자" UI, `Managers` → `UI/ScreenUI/FightUI`)가 전부 배치됨.
  코드에도 씬 `m_Calls` 에도 호출부가 없다.

  남은 위치 — `Map_Kitchen` `Map_Porch` `Map_Room` `Map_Yard` 4개 씬의 동일 경로:

  ```
  UI / ScreenUI / StageInfoPopup      ← StageInfoPopupUI 컴포넌트
     └ PopupRoot (m_IsActive: 0)
        Thumbnail, CharacterNameLabel, IntroLabel,
        ClearBadge, StartButton, CloseButton
  ```

  프리팹 인스턴스가 아니라 씬 직접 배치라 씬마다 개별 삭제해야 한다.
  스크립트를 먼저 지우면 Missing Script 가 남으므로
  → **4개 씬에서 `StageInfoPopup` 오브젝트를 지우고 저장한 뒤** `StageInfoPopupUI.cs` 를 삭제할 것.

### 폐기가 아닌 것

- **`TimelineCutsceneController`** — 씬 배치는 0이지만 죽은 코드가 아니다.
  `Timeline/Cutscene/Bedroom/WallInspect.playable`, `HolyWaterReact.playable` 이 실존하고
  `DialogueTrigger.InspectWall` / `InspectHolyWater` 와 대응한다. **배선만 남은 미완성 기능.**
