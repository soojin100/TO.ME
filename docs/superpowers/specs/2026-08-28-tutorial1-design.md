# 튜토리얼 1 — 설계 문서

작성일: 2026-08-28
기반 기획서: `튜토리얼1.pdf` (17페이지, "엔딩라인&튜토리얼")

## 목표

기획서 p2~p16의 튜토리얼 1 흐름을 구현한다. 시작 화면 분기부터 튜토리얼 전투 종료 직후
대화창까지가 범위다. 모든 수치·순서·텍스트·스프라이트는 데이터(ScriptableObject /
SerializeField / xlsx 시트)로 노출하며 코드에 상수로 박지 않는다.

## 비범위

- **시작 화면** (p2/p3: 새게임/이어하기 분기, 암전 1초, "오늘의 할일은?" 나무판자 버튼) —
  Boot 씬은 별개 사안이라 이번에 손대지 않는다. 튜토리얼 시퀀스는 Boot를 거치지 않고
  Map_Room 진입 시점에 `SeenIntro`만 보고 스스로 시작한다.
- **엔딩 스코어 시스템** (p1: happyEND / nomalEND 누적, 선택지, 엔딩 직전 판정) — 별도 스펙.
  이번엔 `StageSO.countsForEnding` 플래그만 미리 뚫어두고 집계 로직은 만들지 않는다.
- **튜토리얼 2** (조합 및 캐릭터 변화) — 기획서가 명시적으로 다음 문서로 미룸.
- 기존 `c1_*` 대사 수정 — 손대지 않는다.

## 작업 환경 전제

- Unity는 원본 프로젝트를 열며 worktree 수정은 실행 중인 Unity에 반영되지 않는다.
  씬/프리팹 작업은 라이브 프로젝트를 MCP 브리지가 포함된 브랜치(chaerin)로 열어야 가능하다.
- 대사 데이터는 `Assets/CSV/GameData.xlsx`의 `dialogue` 시트에 입력한다.
  `Assets/Editor/XlsxToCsvImporter.cs`가 `dialogue.csv`로 자동 변환한다. CSV를 직접 고치지 않는다.
- `Map_*` 씬의 기존 배치 좌표는 변경하지 않는다. 튜토리얼 오브젝트는 추가만 한다.

## 현재 구현과의 격차

| 기획서 요구 | 현재 상태 |
|---|---|
| ROOM 1-3 구역에서 시작 | `ScreenNavigator.startIndex`는 0. 시작 구역 지정 없음 |
| 강아지 집만 둥근 사각 스포트라이트로 남기고 나머지 어둡게 | 없음 |
| 집 클릭 → 클라이밍 + 펀치스케일 등장 → 중앙 걷기 | 없음. `CharacterWander`가 `Start`에서 즉시 배회 시작 |
| 이름 입력 판자 (대사 이전, 1~12자, 슬라이드 아웃) | `DialogueUI.nameInputPanel`이 있으나 대사 *중간* 트리거(`c1_07`), 길이 제한·슬라이드 없음 |
| 나레이션 모드 (스탠딩 없음, 배경·오브젝트 반투명+입력 차단, 대화창·스킵만) | `MapBusyVisibility`가 버튼·화살표·배회 강아지만 숨김. 배경 암전 없음 |
| 이름 입력 후 강아지를 라인없는 것으로 재배치 | 없음. Map_Room이 `SpriteOutlineMaterial`을 참조 중 |
| 스탠딩 전환 (화남/웃음/분노+흔들림/기본), 곰인형 중앙 출력 | 없음 |
| 맵에 무공격 에너미 배치 → 클릭 시 전투 | `StageButton_1_1`~`1_7` + "싸우자" UI만 있음 |
| 튜토리얼 스테이지: 드랍 X, 인벤토리 X, 엔딩점수 미집계 | `StageManager`가 무조건 드랍 시작. 잠금 개념 없음 |

## 아키텍처

대사 **밖** 연출은 스텝 시퀀스(데이터 주도)로, 대사 **중** 연출은 대사 줄에 붙인 컬럼으로 처리한다.
두 영역을 섞지 않는 이유: 대사 중 연출은 특정 줄과 프레임 단위로 맞아야 하므로 같은 행에 있어야
어긋나지 않고, 대사 밖 연출은 사용자 입력 대기(클릭·이름 입력)를 포함해 시퀀스 제어가 필요하다.

### 스텝 시퀀스 (대사 밖)

`TutorialSequenceSO`는 `[SerializeReference] List<TutorialStep> steps`를 갖는다.
`TutorialStep`은 추상 클래스이고 각 구체 스텝이 `IEnumerator Execute(TutorialContext ctx)`를 구현한다.

ScriptableObject는 씬 오브젝트를 참조할 수 없으므로, 스텝은 **문자열 키**로 대상을 가리키고
씬 오브젝트에는 `TutorialTarget` 컴포넌트가 키를 부여한다. `TutorialSequenceRunner`가
씬 진입 시 키→오브젝트 사전을 만들어 `TutorialContext`로 전달한다. 이 구조 덕분에 SO는
씬 독립적으로 유지되고, 같은 시퀀스를 다른 방에 재사용할 수 있다.

| 스텝 | 노출 파라미터 |
|---|---|
| `MoveToSectionStep` | 기준 오브젝트 키(그 오브젝트가 속한 구역으로 이동), 즉시 이동 여부 |
| `DimSceneStep` | 스포트라이트로 남길 대상 키, 여백, 모서리 반경, 목표 어둡기(0~1), 페이드 시간 |
| `UndimStep` | 페이드 시간 |
| `WaitForClickStep` | 대상 키, 힌트 오브젝트 키(선택) |
| `WaitStep` | 시간(초) |
| `SwapSpriteMaterialStep` | 대상 키, 적용할 머티리얼(비우면 제거) |
| `SummonCharacterStep` | 대상 키, 스폰 위치 키, 스케일 키프레임 배열, 키프레임별 시간, 등장 애니 ID, 이동 목표 키, 이동 속도, 걷기 애니 ID |
| `NameInputBoardStep` | 판자 UI 키, 최소 글자수, 최대 글자수, 슬라이드 인/아웃 시간 |
| `PlayAnimationStep` | 대상 키, 애니 ID, 종료 대기 여부 |
| `PlayDialogueStep` | 대사 시작 ID, 나레이션 모드 사용 여부 |
| `SetWanderStep` | 대상 키, 배회 활성/비활성 |
| `SpawnMapObjectStep` | 프리팹, 배치 위치 키, 진입 대상 `NodeSO`/`StageSO` |

스케일 시퀀스(기획서 `0 → 1.3 → 0.9 → 1.08 → 1.0`)는 `SummonCharacterStep`의 float 배열로
노출한다. 배열 길이가 바뀌어도 코드 수정이 필요 없다.

### 대사 중 연출

`dialogue` 시트에 컬럼 2개를 추가한다. `CsvImporter`는 헤더 기반 파싱이라 기존 행은 영향받지 않는다.

- `standing` — 그 줄에서 표시할 스탠딩 키 (`angry`, `smile`, `rage`, `basic`, `bear`, 빈칸=변경 없음, `none`=숨김)
- `effect` — 그 줄 동안 적용할 연출 키 (`shake`, 빈칸=없음)

`DialogueEntry`에 동일한 두 필드를 추가하고, `StandingCharacterUI`가 `DialogueManager.OnLine`을
구독해 반영한다. 스탠딩 키 → 스프라이트/프레임 매핑과 흔들림 진폭(min/max 픽셀)·주기는
`StandingCharacterUI`의 SerializeField로 노출한다.

## 신규 스크립트

생성 위치: `Assets/Scripts/Tutorial/` (스텝은 `Assets/Scripts/Tutorial/Steps/`).
네임스페이스는 기존 규칙에 맞춰 `TOME.Tutorial`.

| 스크립트 | 책임 |
|---|---|
| `TutorialSequenceSO` | 스텝 배열을 담는 데이터 에셋 |
| `TutorialStep` (abstract) + 구체 스텝 12종 | 각 연출 단위. 파일 1개당 스텝 1개 |
| `TutorialContext` | 러너가 스텝에 넘기는 실행 컨텍스트 (키→오브젝트 사전, 코루틴 호스트) |
| `TutorialSequenceRunner` | 씬 배치. 시퀀스 SO를 받아 스텝을 순차 실행. 완료 시 세이브에 기록 |
| `TutorialTarget` | 씬 오브젝트에 문자열 키 부여 |
| `SpotlightDimmer` | 화면 전체를 어둡게 덮고, 지정 오브젝트 영역만 **둥근 모서리 사각형**으로 뚫어 원래 밝기로 남긴다. 대상 없이 쓰면 균일 암전이 되어 나레이션 모드가 재사용한다 |
| `RoundedSpotlight.shader` | 위 컴포넌트가 쓰는 셰이더. 둥근 사각 컷아웃(SDF) |
| `ScaleSequence` | 키프레임 스케일 보간 (정적 순수 함수). 컴포넌트로 만들지 않는 이유: 씬 배치·와이어링이 필요 없고, 순수 함수라 EditMode에서 바로 검증된다. 재생은 `SummonCharacterStep`이 직접 돌린다 |
| `NameInputBoardUI` | 판자 슬라이드 인/아웃, 글자수 검증, 확정 콜백 |
| `NarrationModeController` | 나레이션 구간 동안 `SpotlightDimmer`를 스포트라이트 없이(=균일) 켜고 콜라이더를 꺼 입력을 막는다. 종료 시 원복 |
| `StandingCharacterUI` | 스탠딩 키→스프라이트 전환, 랜덤 흔들림 |
| `MapTutorialEnemy` | 맵 배치 무공격 에너미. 클릭 시 지정 스테이지 진입 |

## 기존 스크립트 수정

| 파일 | 수정 |
|---|---|
| `Data/Dialogue/DialogueEntry.cs` | `standing`, `effect` 필드 추가 |
| `UI/Dialogue/DialogueUI.cs` | 나레이션 줄(화자 비었거나 `나레이션`)이면 초상화 숨김 |
| `Data/Stages/StageSO.cs` | `allowItemDrops` / `allowInventory` / `countsForEnding` bool 3개 추가 (기본 true) |
| `Managers/StageManager.cs` | `allowItemDrops`가 false면 `ItemDropManager.Begin` 생략 |
| `UI/Combat/HudUI.cs` | `allowInventory`가 false면 인벤토리 진입 버튼 비활성 |
| `Cute/CharacterWander.cs` | `autoStartWander` 플래그 + `BeginWander()` 공개 메서드. 소환 연출 전 배회 금지 |
| `Map/Flow/TutorialIntroController.cs` | `Start`에서 `c1_01`을 자동 재생하던 경로 제거. 대사 시작 책임은 `TutorialSequenceRunner`의 `PlayDialogueStep`으로 이전. "싸우자" UI 공용 컨트롤러 역할(`Show`, `OnFightClicked`)은 그대로 유지 |

`SaveSystemManager`와 `BootstrapEntry`는 수정하지 않는다. 튜토리얼 실행 여부는 기존
`SeenIntro`(첫 실행 튜토리얼 시청 여부)로 판단하며, 이 판단은 Boot 씬이 아니라
Map_Room의 `TutorialSequenceRunner`가 자기 `Start`에서 직접 한다 — 시작 화면 흐름과
분리돼 있어 Boot 씬을 나중에 어떻게 바꾸든 튜토리얼은 영향받지 않는다.

## 대사 데이터

`GameData.xlsx`의 `dialogue` 시트에 `tut1_` 접두 ID로 신규 행을 추가한다. 기존 `c1_*`는 보존.
기획서의 `XXX`는 `{name}` 토큰으로 치환한다 (기존 치환 로직 재사용).
기획서에서 각 대사 앞에 붙은 `-`는 "대사창 한 줄"의 구분 기호로 읽고, 줄마다 별도 행으로 만들되
텍스트에는 포함하지 않는다 (기존 `c1_*` 행들도 `-` 없이 저장되어 있음).

### 나레이션 (p8) — `tut1_nar_01` ~ `tut1_nar_07`

speaker = `나레이션`, standing = `none`

```
언제나처럼 평화로운 {name}의 하우스.
주인님은 사냥 나가고 {name} 혼자 집을 지키게 된지 3시간째.
집에만 있는 다고 노는게 아니다!
할 일이 얼마나 많은지 말로는 전부 말할 수가 없다.
마당부터 방까지 순찰,
대문 앞에서 10분동안 주인님 그리워하기,
주방에 먹을게 없나 찬장 열어보기-...
```

### 화내는 강아지 (p9) — `tut1_dog_01` ~ `tut1_dog_07`

speaker = `{name}`, 첫 줄 standing = `angry`

```
잠시만!
지금  아주 개인적인 사생활이 폭로되는 기분이었는데?
아잇, 오늘 마가 꼈나
주방에 먹은 건 하나도 없고
야채만 잔뜩 있었다구
치커리? 샐러드? 그런걸 인간들은 왜 먹는거야?
입맛만 버렸네!
```

### 웃는 강아지 (p10) — `tut1_dog_08` ~ `tut1_dog_10`

```
오전 루틴은 망쳤지만 오후까진 망칠 순 없지!   ← 다음 줄부터 standing = smile
열심히 주인을 삥뜯어서 산 인형~
우리 곰뻥이 어디 갓-                          ← 이 줄 끝에서 standing = none (스탠딩 사라짐)
```

### 곰인형 등장 (p11) — `tut1_bear_01`

standing = `bear`. 목이 덜렁덜렁한 곰인형을 화면 중앙에 출력. 텍스트 없음.

### 분노 (p12) — `tut1_rage_01` ~ `tut1_rage_09`

첫 줄 standing = `rage`, effect = `shake` (전 줄 유지)

```
으아아아아아아
으아아아
으아아아아아아아악
곰뻥이가 죽었다!
곰뻥이 모가지가 똑떨어지려고 하잖아!
아니 이미 뚝 떨어졌잖아?!
안 돼!
이럴 순 없어!!!!!!!!!!
이럴 순 없,
```

### 진정 (p13) — `tut1_calm_01` ~ `tut1_calm_06`

첫 줄 standing = `basic`, effect 비움 (흔들림 정지)

```
잠시만
이거 AS되는 인형 아니야?
처음 데려왔을 때 제품설명서 꼼꼼하게 읽을걸
...
후회해도 어쩔 수 없지...
이렇게 된거 집먼지놈들을 패서 알아내야겠다
```

### UI 문구

- 이름 입력 판자 라벨: `멋진 내이름`

대사 흐름이 아니라 UI 라벨이므로 씬의 TMP 컴포넌트에 직접 넣는다.
기획서 p2의 `오늘의 할일은?` / `최애 인형과 놀기!`는 Boot 씬 소관이라 이번 범위 밖이다.

## 전체 흐름

```
Map_Room 진입 (Boot 씬 흐름은 현재 그대로 — 이번 범위 밖)
  ├ SeenIntro == true  → 시퀀스 미실행, 배회 즉시 시작
  └ SeenIntro == false → 아래 시퀀스 실행

Map_Room 튜토리얼 시퀀스 (TutorialSequenceSO 스텝 순서):
  1. MoveToSection  — StageButton_1_3이 속한 구역으로 (기획서 p4 "ROOM 1-3에서 시작")
  2. DimScene       — 강아지 집만 둥근 사각 스포트라이트로 남기고 나머지 어둡게
  3. WaitForClick   — 강아지 집 클릭 대기
  4. SummonCharacter— 클라이밍 애니 + 스케일 0→1.3→0.9→1.08→1.0,
                      집 중앙에서 소환 → 화면 중앙으로 걷기(걷기 애니)
  5. NameInputBoard — 판자 슬라이드 인 → 1~12자 입력 → 확정 → 슬라이드 아웃
  6. Undim          — 어둡기 원복 (기획서 p7 "불투명도 사라진 다음")
  7. SwapSpriteMaterial — 강아지를 라인없는 것으로 교체 (기획서 p7)
  8. Wait 0.3s
  9. PlayAnimation  — 점프 1회
 10. PlayDialogue   — tut1_nar_01 시작, 나레이션 모드 ON
                      (배경·오브젝트 반투명 + 입력 차단, 대화창·스킵만 작동)
                      대사 진행 중 standing/effect 컬럼으로 스탠딩 전환·흔들림
 11. SetWander      — 배회(다마고치) 활성화
 12. SpawnMapObject — 무공격 에너미 배치, 클릭 대상 = 튜토리얼 Node/Stage

튜토리얼 스테이지: allowItemDrops=false, allowInventory=false, countsForEnding=false
  └ 승리 → 맵 복귀 → postDialogueId 대사 (튜토리얼 2 경계)
```

## 판단 근거를 남기는 결정

1. **에너미 클릭 = 즉시 진입**. 기획서 p14가 "클릭시 ... 게임맵을 출력"이라 기존 "싸우자!" UI를
   거치지 않는다. `MapTutorialEnemy`가 `GameManager.EnterStage`를 직접 호출한다.
   일반 스테이지 진입 경로(`StageNodeButton` → `MapFlowController` → 싸우자 UI)는 그대로 둔다.
2. **나레이션 모드의 "투명화" = 반투명(암전)이지 소멸이 아니다**. 원문만 보면 알파 0으로
   완전히 지우는 해석도 가능하지만, 기획서 목업 p7 우측 ~ p13이 **6장 연속으로 방이
   어둡게 보이는 상태**를 그리고 있고 p8에서는 배회 강아지도 그대로 보인다. 따라서
   화면 전체를 어둡게 덮고(`SpotlightDimmer`, 스포트라이트 없음) 콜라이더를 꺼 입력만 막는다.
   같은 문장의 "스탠딩 캐릭터 아무것도 출력되지 않고"는 큰 초상화를 가리키며(p9부터 등장),
   이는 `standing` 컬럼을 비워 두는 것으로 충족된다.
   오브젝트를 파괴하거나 `SetActive(false)`하지 않는 이유: 모드 종료 후 원복해야 하고,
   `MapBusyVisibility`가 이미 `SetActive` 토글을 쓰고 있어 겹치면 복원 상태가 꼬인다.
3. **디밍은 오브젝트 색이 아니라 화면 오버레이 + 둥근 사각 컷아웃**. 기획서 p4 예시 이미지가
   강아지 집 *주변 영역*(뒤 벽·바닥 포함)을 통째로 밝게 남긴다. 오브젝트 단위로 색을 낮추면
   강아지 집 스프라이트만 밝고 뒤 바닥이 어두워져 오려낸 것처럼 보인다. 모서리는 각지지 않고
   둥글어야 한다. 대상은 강아지 집에 한정되지 않으며, 어떤 오브젝트든 키만 바꾸면 그 영역이
   스포트라이트가 된다.
4. **"ROOM 1-3에서 시작"의 해석**. p3(기존 기록)과 p4(튜토리얼)의 목업 프레이밍이 동일하고,
   그 화면 카펫 위의 `1-3` 사각형은 씬에 실재하는 `StageButton_1_3`이다. 따라서
   "`StageButton_1_3`이 배치된 구역에서 시작"으로 읽는다. 구역 인덱스를 상수로 박지 않고
   `ScreenNavigator.SectionIndexAtWorldX(버튼의 월드 X)`로 역산한다 — 구역 수가 화면 비율에
   따라 런타임에 계산되므로 고정 인덱스는 기기마다 어긋난다.
5. **"다마고치 강아지는 라인없는 걸로 재배치"(p7)는 머티리얼 교체로 본다**. 프로젝트에
   `Assets/Shaders/SpriteOutline.shader`·`SpriteOutlineMaterial.mat`이 있고 Map_Room이 실제로
   이 머티리얼을 참조한다(`Sprites/Map/Hallway/`에도 `Outline`/`NoOutline` 폴더 쌍이 있음).
   따라서 오브젝트를 갈아끼우는 대신 대상의 `SpriteRenderer` 머티리얼을 제거(또는 지정
   머티리얼로 교체)한다. 스텝이 머티리얼 필드를 노출하므로 다른 처리가 필요하면 데이터로 바꾼다.
6. **결과창은 유지한다**. 기획서 p16 "플레이가 끝나면 바로 대화창으로 넘어감"의 "바로"는
   맵 복귀 직후를 뜻하는 것으로 보고 기존 `ResultScreenUI` 흐름을 그대로 둔다.
7. **플레이스홀더 아트**. 프로젝트에 없는 세 가지를 임시 대체하되 전부 SerializeField로 노출해
   실제 아트가 나오면 Inspector 교체만으로 끝나게 한다.
   - 나무판자 UI → 단색 라운드 패널
   - 목이 덜렁덜렁한 곰인형 → `Assets/Sprites/Items/item_Bear.png` 확대
   - 웃는 강아지 스탠딩 → `Sprites/Characters/Dialogue/Talking` 프레임 재사용
8. **`countsForEnding` 플래그를 지금 넣는 이유**. 기획서 p15가 "해당 스테이지는 무조건 적인 성공을
   가정하였기에 엔딩 점수가 지정되지 않음"이라고 명시한다. 엔딩 시스템 구현 시 튜토리얼 스테이지를
   따로 찾아다니지 않도록 플래그만 미리 심는다. 집계 로직은 이번 범위 밖.

## 비기능 요건

- 스텝 실행은 코루틴 기반이며 `Time.unscaledDeltaTime`을 쓴다. 대화·연출 중 `timeScale`이
  0이 되는 경로(`CombatManager.Pause`)와 겹쳐도 멈추지 않게 한다.
- `SpotlightDimmer`는 화면 오버레이 하나만 조작하므로 오브젝트별 색 복원 기록이 필요 없다.
  `NarrationModeController`가 끄는 콜라이더 목록만 리스트로 보관해 원복하며, 매 프레임 `Find*` 하지 않는다.
- 모든 이벤트 구독은 `OnDisable`/`OnDestroy`에서 해제한다 (기존 패턴).
- 매니저 접근은 전부 null 가드 (기존 패턴).
- 튜토리얼 완료 여부는 기존 `SaveSystemManager.SeenIntro`를 재사용한다. 새 필드를 만들지 않는다.
  `TutorialSequenceRunner`가 시퀀스 완료 시 `MarkIntroSeen()`을 호출한다 (기존에는
  `TutorialIntroController.OnBattleStart`가 호출했다 — 그 호출은 제거한다).
- `DialogueUI`의 기존 `NameInput` 트리거 경로(`nameInputPanel`)는 그대로 둔다. 튜토리얼 1은
  대사 **이전**에 `NameInputBoardStep`으로 이름을 받으므로 두 경로가 동시에 뜨지 않는다.
  기존 `c1_07`은 계속 옛 경로를 쓴다.

## 테스트

- **EditMode 단위 테스트**: `TutorialSequenceSO` 스텝 직렬화 왕복 검증
  (`[SerializeReference]` 다형 리스트가 스텝 타입을 보존하는지) — 이 구조의 유일한 비자명 위험.
- **수동 (Play 모드)**: Map_Room 씬에서 직접 Play 한다 (Boot를 거치지 않는다).
  1. 세이브 삭제 후 시퀀스 전체 → 튜토리얼 전투 → 복귀 대화
  2. 세이브 있는 상태(SeenIntro=true)에서 시퀀스 미실행, 배회 즉시 동작
  3. 이름 0자/13자 입력 시 확정 거부, 1자/12자는 통과
  4. 나레이션 모드 진입·종료 후 배경이 원래 밝기로 돌아오고 클릭이 다시 먹는지
  5. 디밍 시 강아지 집 주변이 **둥근 모서리 사각형**으로 밝게 남는지 (각지면 안 됨)
  6. 이름 입력 후 강아지 외곽선이 사라지는지
  7. 시작 시 `StageButton_1_3`이 보이는 구역에 카메라가 있는지
  8. 튜토리얼 스테이지에서 아이템이 드랍되지 않고 인벤토리 버튼이 안 눌리는지

## 작업 순서

1. 기존 스크립트 수정 (`DialogueEntry`, `CsvImporter`, `StageSO`, `StageManager`,
   `HudUI`, `CharacterWander`, `DialogueUI`, `TutorialIntroController`) — 컴파일 통과 확인
2. 신규 스크립트 작성 (스텝 프레임워크 → 셰이더/연출 컴포넌트 → 스텝)
3. `GameData.xlsx` `dialogue` 시트에 `tut1_*` 행 추가 → CSV 자동 변환 확인
4. `TutorialSequenceSO` 에셋 생성 + 스텝 구성
5. 기존 `Stage_Tutorial.asset` 수정 (적을 `Enemy_NoAttack` 1마리로, 제약 플래그 off).
   `Enemy_NoAttack.asset`·`Node_Tutorial.asset`은 이미 존재하고 서로 연결돼 있어 수정 불필요
6. 무공격 에너미 프리팹 생성 (`MapTutorialEnemy` 컴포넌트 + `enemy_NoAttack.png`)
7. Map_Room 씬: `TutorialTarget` 키 부여, 이름 판자 UI, 스탠딩 UI, 디머·나레이션·러너 배치
8. EditMode 테스트 + Play 모드 전체 검증

3~8번은 Unity를 라이브 프로젝트(chaerin 브랜치)로 열어야 진행할 수 있다.
