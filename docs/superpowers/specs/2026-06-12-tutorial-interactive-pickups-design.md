# 튜토리얼 실제 인터랙션 컷신 — 설계 문서

작성일: 2026-06-12
대상: ghost1 튜토리얼 대사(gp34~gp70)의 사물 인터랙션 단계

## 1. 목표

튜토리얼 대사가 지시하는 행동(성수 줍기, 당근 줍기, 침대 밀기, 별 줍기, 조합)을
**플레이어가 실제로 수행**하게 만들고, 각 행동 완료까지 대사를 대기시킨다.
현재 이 줄들은 트리거가 없어 텍스트로만 흐르고, "넘어가기"를 누르면 그대로 스킵된다.

벽 낙서(gp34)·문 충격(gp48)은 줍는 대상이 없는 **패시브 인스펙트 컷신**이라 기존 방식 유지.

## 2. 현재 상태

- 대사 트리거 흐름: `DialogueManager.HandleTrigger`가 `OnInteractionRequested(trigger)` 발생 후
  `ResumeFromInteraction()`까지 대기. 컷신 컨트롤러가 이를 구독해 처리.
- `TimelineCutsceneController`(Room_Bedroom): trigger 3=벽, 4=성수, 5=문. 카메라 포커스 + (선택)PlayableDirector.
- `MapPickup`: 탭 선택 → 재탭 줍기 → `InventoryManager.Add` + 저장. **단 `DialogueManager.IsPlaying` 중엔 입력 차단**([MapPickup.cs:55]).
- `MergeCraftManager`: 슬롯 배치 후 `Craft()` → `OnCraftSucceeded(CharacterSO)` 이벤트.
- 씬 오브젝트: **Carrot 존재(단 MapPickup 아님)**, **Bedding(침대) 존재**, **Star 없음**.
- 레시피: Red/Angel/King/Wizard만 존재 — **당근+별→떡내놔 레시피 없음**.
- `DialogueTrigger` enum: None/NameInput/StartBattle/InspectWall/InspectHolyWater/InspectDoor.

## 3. 공통 기반 (선결 1회)

### 3.1 대사 중 인터랙션 언락
현재 `IsPlaying`이 모든 줍기/조합 입력을 막는다. "특정 행동을 기다리는 단계"에서만
**지정된 인터랙션을 허용**하는 게이트가 필요하다.

- `DialogueManager`에 `public bool AwaitingInteraction { get; private set; }` 추가
  (Inspect*/Catch*/Move*/Merge* 트리거의 대기 구간에서 true).
- `MapPickup.OnMouseDown`의 차단 조건을 완화: `IsPlaying && !(이 픽업이 현재 언락 대상)`일 때만 차단.
  - 언락 대상 식별: `TutorialStepController`가 활성 단계의 대상 픽업을 정적/매니저 경유로 표시
    (예: `DialogueManager.UnlockedPickupId` 또는 컨트롤러가 대상 픽업에 직접 `SetUnlocked(true)`).

### 3.2 TutorialStepController (신규)
`TimelineCutsceneController`를 일반화. `OnInteractionRequested`를 받아 단계 종류별로 처리.

각 Entry:
- `trigger`
- `cameraTarget`, `zoomOrthoSize`, `focusOffset`, `moveDuration` (포커스 — 기존과 동일)
- `director`(선택, 효과 연출 — 예: 성수 감전)
- `stepKind`: `Pickup | Merge | DragReveal`
- 종류별 참조: `MapPickup targetPickup` / `CharacterSO mergeResult` / `DraggableReveal bed` + `MapPickup revealedStar`

흐름:
1. 카메라 포커스(+필요 시 director 재생·종료 대기 — 성수 감전 연출).
2. 대상 인터랙션 언락 + 하이라이트.
3. 완료 이벤트 감지 → 언락 해제 → `ResumeFromInteraction()`.

> 인스펙트(벽·문)는 위험 최소화를 위해 기존 `TimelineCutsceneController`에 그대로 둔다.
> 성수는 줍기가 추가되므로 `TimelineCutsceneController`의 trigger 4 엔트리를 제거하고 `TutorialStepController`로 이관.

## 4. 단계별 설계

### 4.1 성수 줍기 (gp38) — 줍기 + 감전 연출 유지
- 트리거: 기존 `InspectHolyWater` 재사용(의미 전환).
- 처리: 카메라 포커스 → `HolyWaterReact` director 재생(유령 움찔, gp39 내러티브) → 성수 픽업 언락 →
  플레이어 탭하여 줍기 → `OnPicked` → 재개.
- 신규: 성수 ItemSO + 씬 성수 오브젝트에 `MapPickup`(현재는 인스펙트 대상일 뿐 줍기 아님).

### 4.2 당근 줍기 (gp64)
- 트리거: 신규 `CatchCarrot`.
- 처리: Carrot 포커스 → 픽업 언락 → 탭하여 줍기 → 재개.
- 신규: 당근 ItemSO + Carrot 오브젝트에 `MapPickup`/`Collider2D`/`SpriteHighlight` 부착.

### 4.3 침대 밀기 → 별 노출 (gp67)
- 트리거: 신규 `MoveBed`.
- 처리: Bedding 포커스 → 신규 `DraggableReveal`로 침대를 일정 거리 밀면 → Star 오브젝트 노출(active) → 재개.
  - gp68 "(드래그해 밀자 별 조각이 드러났다)" = **노출까지가 이 단계**. 줍기는 다음 단계.
- 신규: `DraggableReveal` 컴포넌트(기존 드래그 시스템 없음), Star 오브젝트 + 별 ItemSO + `MapPickup`(노출 시 활성).

### 4.4 조합 (gp69) — 별 줍기 포함
- 트리거: 신규 `MergeItems`.
- 처리: 별 픽업 언락 + 조합창 접근 허용 → 플레이어가 별을 줍고(당근은 이미 보유) 당근+별 조합 →
  `OnCraftSucceeded` 결과가 떡내놔면 재개.
  - 별 줍기는 **이 단계에서** 일어난다(gp68~gp70 사이 별도 줍기 대사 줄이 없음).
- 신규: 당근+별→떡내놔 RecipeSO. 대사 중 조합창 접근 허용(IsPlaying 게이트 완화 범위에 포함).

## 5. 데이터/배선 변경

- `DialogueTrigger`에 `CatchCarrot`, `MoveBed`, `MergeItems` 추가(`InspectHolyWater`는 의미 전환).
- `dialogue.csv` 트리거 컬럼: gp38=InspectHolyWater(유지), gp64=CatchCarrot, gp67=MoveBed, gp69=MergeItems.
- 신규 ItemSO 3종(성수·당근·별), 신규 RecipeSO 1종(당근+별→떡내놔), Star 오브젝트.
- Room_Bedroom: Carrot/성수/Star에 MapPickup 구성, 침대에 DraggableReveal, `TutorialStepController` 엔트리 추가, 성수 엔트리를 TimelineCutsceneController에서 이관.

## 6. 빌드 순서 (분리)

1. **공통 기반**: 인터랙션 언락 게이트 + `TutorialStepController` 골격.
2. **성수 줍기**(기존 트리거 전환 — 효과 재사용으로 검증 쉬움).
3. **당근 줍기**(가장 단순한 신규 픽업).
4. **조합**(레시피 + 조합창 언락 + 별 줍기).
5. **침대 드래그 + 별 노출**(신규 드래그 메커니즘 — 가장 큰 신규 작업, 마지막).

각 단계는 독립 트리거라 점진 통합·검증 가능.

## 7. 리스크 / 미확정

- **인터랙션 언락 게이트**가 교차 변경(공통 선결). 잘못하면 일반 맵 줍기에 영향 — 대상 한정 필수.
- **드래그 시스템 신규**(침대): 입력/임계거리/되돌림 등 설계 필요.
- **Star 에셋·당근/성수/별 ItemSO·레시피 신규 제작** 필요(아트 placeholder 가능).
- **별 줍기 타이밍**: 조합 단계(gp69)에서 줍는 것으로 확정(별도 대사 줄 없음) — 검토 시 확인.
- 조합창을 대사 중 여는 UX(버튼/유도) 확정 필요.

## 8. 테스트 (수동, Play 모드)

- gp38: 성수 포커스+감전 후 탭하여 줍힘 → gp39 진행.
- gp64: 당근 탭하여 줍힘 → gp65 진행.
- gp67: 침대 밀어 별 노출 → gp68 진행.
- gp69: 별 줍고 당근+별 조합→떡내놔 → gp70 진행.
- "넘어가기"가 각 인터랙션 지점에서 멈추는지(트리거에서 fastForward 해제) 확인.
- 일반 맵(튜토리얼 외)에서 줍기/조합이 정상(언락 게이트 부작용 없음).
