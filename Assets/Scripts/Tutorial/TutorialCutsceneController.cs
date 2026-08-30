using System.Collections;
using UnityEngine;
using TOME.Core;
using TOME.Cutscene;
using TOME.Dialogue;
using TOME.Map;
using TOME.Progression;
namespace TOME.Tutorial
{
    /// <summary>튜토리얼 1의 신규 컷신을 대사 트리거로 처리한다.
    /// 기존 CutsceneInteractionController 와 같은 방식으로 DialogueManager.OnInteractionRequested 를 받아
    /// 연출을 재생하고 ResumeFromInteraction 으로 대사를 재개한다.
    /// 흐름 자체는 dialogue 시트의 줄 순서와 trigger 컬럼이 결정한다 — 별도 시퀀스 데이터를 두지 않는다.
    ///
    /// Map_Room 씬의 Managers 아래에 배치한다(씬 수명과 함께 생성/파괴).</summary>
    public class TutorialCutsceneController : MonoBehaviour
    {
        [Header("공통")]
        [SerializeField] SpotlightDimmer dimmer;
        [Tooltip("배회 강아지(CharacterWander 보유). 소환 전에는 꺼 둔다.")]
        [SerializeField] GameObject dog;

        [Header("TutorialSummon — 기획서 p4~p5")]
        [Tooltip("암전에서 밝게 남길 대상. 이 오브젝트를 클릭하면 강아지가 나온다.")]
        [SerializeField] GameObject dogHouse;
        [Tooltip("카메라를 이 오브젝트가 속한 구역으로 먼저 옮긴다(기획서 p4 \"ROOM 1-3에서 시작\"). 비우면 이동 안 함.")]
        [SerializeField] Transform sectionAnchor;
        [Tooltip("강아지가 걸어갈 목적지.")]
        [SerializeField] Transform walkTarget;

        [Space]
        [Tooltip("대상 바운즈 바깥으로 더 밝게 남길 여백(월드 단위).")]
        [Min(0f)] [SerializeField] float spotlightPadding = 0.35f;
        [Tooltip("스포트라이트 모서리 반경(월드 단위). 각지지 않게 0보다 크게.")]
        [Min(0f)] [SerializeField] float spotlightCornerRadius = 0.6f;
        [Range(0f, 1f)] [SerializeField] float dimAmount = 0.55f;
        [Min(0f)] [SerializeField] float dimDuration = 0.4f;

        [Space]
        [Tooltip("강조 시 카메라 orthographicSize (작을수록 확대). 기존 CutsceneInteractionController 포커스 모드와 같은 방식.")]
        [Min(0.1f)] [SerializeField] float zoomOrthoSize = 5f;
        [Tooltip("줌인/줌아웃 각 구간 소요 시간(초).")]
        [Min(0f)] [SerializeField] float zoomDuration = 0.4f;
        [Tooltip("줌인 중심 보정(월드). 대상이 화면 가장자리에 걸릴 때 중심을 옮겨 빈 영역 노출을 막는다.")]
        [SerializeField] Vector2 zoomOffset = Vector2.zero;

        [Space]
        [Tooltip("등장 스케일 키프레임. 1.0 = 배치된 원본 크기 (기획서 p5).")]
        [SerializeField] float[] scaleKeys = { 0f, 1.3f, 0.9f, 1.08f, 1.0f };
        [Tooltip("키 사이 각 구간의 소요 시간(초). 키가 n개면 n-1개.")]
        [SerializeField] float[] scaleDurations = { 0.12f, 0.08f, 0.06f, 0.06f };
        [Tooltip("등장 시 재생할 CharacterWander 등록 애니 ID.")]
        [SerializeField] string appearAnimationId = "climbing";
        [Tooltip("이동 중 켜 둘 Animator bool 파라미터.")]
        [SerializeField] string walkParam = "walk";
        [Min(0f)] [SerializeField] float walkSpeed = 2f;
        [Tooltip("목적지 도착 판정 거리(월드 단위).")]
        [Min(0.001f)] [SerializeField] float arriveThreshold = 0.05f;

        [Header("TutorialDogLine — 기획서 p7")]
        [Tooltip("이 접미사로 끝나는 SpriteRenderer가 외곽선 레이어다. 끄면 라인이 사라진다.")]
        [SerializeField] string lineRendererSuffix = "_W";

        [Header("TutorialBear — 기획서 p11")]
        [Tooltip("목이 덜렁덜렁한 곰인형 이미지. 평소 비활성.")]
        [SerializeField] GameObject bearDoll;
        [Tooltip("곰인형을 보여주는 시간(초).")]
        [Min(0f)] [SerializeField] float bearHoldSeconds = 1.4f;

        [Header("TutorialEnemy — 기획서 p14")]
        [SerializeField] GameObject enemyPrefab;
        [SerializeField] Transform enemySpawnPoint;
        [SerializeField] NodeSO enemyNode;
        [SerializeField] StageSO enemyStage;
        [Tooltip("에너미 강조 사각형의 여백 — 월드 단위가 아니라 대상 크기에 대한 비율. " +
                 "0.08이면 대상보다 8% 넓게, 음수면 대상보다 타이트하게. " +
                 "비율이라 크기가 다른 에너미가 와도 값을 다시 잡을 필요가 없다.")]
        [SerializeField] float enemySpotlightPaddingRatio = 0.08f;
        [Tooltip("모서리 둥글기 — 짧은 변 절반에 대한 비율(0~1). 이것도 대상 크기를 따라간다.")]
        [Range(0f, 1f)] [SerializeField] float enemySpotlightCornerRatio = 0.35f;

        void Awake()
        {
            // 소환 전까지 다마고치 강아지는 보이지 않는다 (기획서 p5).
            if (dog != null) dog.SetActive(false);
        }

        bool _subscribed;

        // OnEnable 이 DialogueManager.Awake 보다 먼저 돌 수 있어 Start 에서 한 번 더 시도한다
        // (DialogueUI 와 같은 패턴). 구독을 놓치면 트리거에서 대사가 영원히 멈춘다.
        void OnEnable() => TrySubscribe();
        void Start()    => TrySubscribe();

        void TrySubscribe()
        {
            if (_subscribed || DialogueManager.I == null) return;
            DialogueManager.I.OnInteractionRequested += OnInteraction;
            _subscribed = true;
        }

        void OnDisable()
        {
            // 컷신 도중 꺼져도 테두리가 한 오브젝트에 묶인 채로 남지 않게 한다.
            MapBusyVisibility.SetHighlightFocus(null);
            if (!_subscribed) return;
            if (DialogueManager.I != null) DialogueManager.I.OnInteractionRequested -= OnInteraction;
            _subscribed = false;
        }

        void OnInteraction(DialogueTrigger trigger)
        {
            switch (trigger)
            {
                case DialogueTrigger.TutorialSummon:  StartCoroutine(SummonRoutine());  break;
                case DialogueTrigger.TutorialDogLine: StartCoroutine(DogLineRoutine()); break;
                case DialogueTrigger.TutorialEnemy:   StartCoroutine(EnemyRoutine());   break;
                case DialogueTrigger.TutorialBear:    StartCoroutine(BearRoutine());    break;
                default: return;   // 다른 트리거는 기존 컨트롤러가 처리한다
            }
        }

        // 기획서 p4~p6: 구역 이동 → 강아지 집 확대 + 나머지 암전 → 집 클릭 대기 → 줌아웃 →
        //               클라이밍 + 펀치스케일 등장 → 중앙으로 걷기
        IEnumerator SummonRoutine()
        {
            yield return MoveCameraToSection();

            var cam = Camera.main;
            Vector3 baseCamPos  = cam != null ? cam.transform.position : Vector3.zero;
            float   baseOrtho   = cam != null ? cam.orthographicSize : 0f;

            // 대상으로 카메라를 확대한다 — 기존 CutsceneInteractionController 포커스 모드와 같은 방식.
            Require(dogHouse, nameof(dogHouse), "강아지 집 확대·강조");
            Require(dimmer,   nameof(dimmer),   "주변 암전(스포트라이트)");
            if (cam != null && dogHouse != null)
            {
                Vector3 focus = new(dogHouse.transform.position.x + zoomOffset.x,
                                    dogHouse.transform.position.y + zoomOffset.y, baseCamPos.z);
                yield return CameraTween.PanZoom(cam, baseCamPos, baseOrtho, focus, zoomOrthoSize, zoomDuration);
            }

            if (dimmer != null)
                yield return dimmer.DimRoutine(dogHouse, spotlightPadding, spotlightCornerRadius,
                                               dimAmount, dimDuration);

            // 기획서 p4~p5: 이 구간에서 누를 수 있는 건 강아지 집뿐이다.
            // 다른 오브젝트에 호버 테두리가 뜨면 그것도 누를 수 있는 것처럼 보이므로 강아지 집만 남긴다.
            MapBusyVisibility.SetHighlightFocus(dogHouse);
            yield return WaitForDogHouseClick();
            MapBusyVisibility.SetHighlightFocus(null);

            // 확대를 풀고 원래 구역 화면으로 돌아온 뒤 강아지가 나온다.
            if (cam != null)
                yield return CameraTween.PanZoom(cam, cam.transform.position, cam.orthographicSize,
                                                 baseCamPos, baseOrtho, zoomDuration);

            yield return SummonDog();

            DialogueManager.I?.ResumeFromInteraction();
        }

        /// <summary>연출에 꼭 필요한 참조가 연결됐는지 확인한다. 비어 있으면 경고를 남기고 false.
        /// 참조가 빠지면 그 연출만 조용히 건너뛰고 대사는 그대로 진행되기 때문에,
        /// 로그가 없으면 나중에 "왜 안 나오지"를 추적할 단서가 전혀 남지 않는다.</summary>
        bool Require(UnityEngine.Object reference, string fieldName, string what)
        {
            if (reference != null) return true;
            Debug.LogWarning($"[Tutorial] {fieldName} 이(가) 비어 있어 {what}을(를) 건너뜁니다. " +
                             $"{name} 의 인스펙터에서 연결하세요.", this);
            return false;
        }

        IEnumerator MoveCameraToSection()
        {
            if (!Require(sectionAnchor, nameof(sectionAnchor), "튜토리얼 구역으로 카메라 이동")) yield break;
            var nav = ScreenNavigator.Instance;
            if (nav == null)
            {
                Debug.LogWarning("[Tutorial] 씬에 ScreenNavigator가 없어 구역 이동을 건너뜁니다.", this);
                yield break;
            }

            // 구역 인덱스를 상수로 박지 않고 앵커의 월드 X로 역산한다 —
            // 구역 수가 화면 비율에 따라 런타임에 계산되므로 고정 인덱스는 기기마다 어긋난다.
            var move = nav.MoveToSection(nav.SectionIndexAtWorldX(sectionAnchor.position.x));
            if (move != null) yield return move;
        }

        IEnumerator WaitForDogHouseClick()
        {
            // dogHouse 누락은 SummonRoutine 진입부에서 이미 경고했다. 여기서는 대기 없이 통과시킨다.
            if (dogHouse == null) yield break;

            var cam = Camera.main;
            while (true)
            {
                if (Input.GetMouseButtonDown(0) && cam != null)
                {
                    Vector3 wp = cam.ScreenToWorldPoint(Input.mousePosition);
                    var hit = Physics2D.OverlapPoint(new Vector2(wp.x, wp.y));
                    if (hit != null &&
                        (hit.gameObject == dogHouse || hit.transform.IsChildOf(dogHouse.transform)))
                        yield break;
                }
                yield return null;
            }
        }

        IEnumerator SummonDog()
        {
            if (!Require(dog, nameof(dog), "강아지 등장")) yield break;

            if (dogHouse != null) dog.transform.position = dogHouse.transform.position;
            dog.SetActive(true);

            var wander = dog.GetComponent<CharacterWander>();
            if (wander != null && !string.IsNullOrEmpty(appearAnimationId))
                wander.PlayAnimation(appearAnimationId);

            yield return PunchScale(dog.transform);

            if (Require(walkTarget, nameof(walkTarget), "강아지가 중앙으로 걸어오기"))
                yield return Walk(dog.transform, walkTarget.position);
        }

        // 배치된 원본 스케일을 1.0 기준으로 삼아 키프레임 배율을 곱한다.
        IEnumerator PunchScale(Transform t)
        {
            Vector3 baseScale = t.localScale;
            float total = ScaleSequence.TotalDuration(scaleKeys, scaleDurations);
            if (total <= 0f) { t.localScale = baseScale; yield break; }

            float elapsed = 0f;
            while (elapsed < total)
            {
                elapsed += Time.unscaledDeltaTime;
                t.localScale = baseScale * ScaleSequence.Evaluate(scaleKeys, scaleDurations, elapsed);
                yield return null;
            }
            t.localScale = baseScale;
        }

        IEnumerator Walk(Transform t, Vector3 destination)
        {
            destination.z = t.position.z;

            // 진행 방향으로 좌우 반전 — CharacterWander와 같은 규약(부호 반전).
            float dx = destination.x - t.position.x;
            if (Mathf.Abs(dx) > 0.01f)
            {
                Vector3 s = t.localScale;
                t.localScale = new Vector3(Mathf.Abs(s.x) * -Mathf.Sign(dx), s.y, s.z);
            }

            var animator = t.GetComponentInChildren<Animator>();
            if (animator != null && !string.IsNullOrEmpty(walkParam)) animator.SetBool(walkParam, true);

            while (Vector3.Distance(t.position, destination) > arriveThreshold)
            {
                t.position = Vector3.MoveTowards(t.position, destination, walkSpeed * Time.unscaledDeltaTime);
                yield return null;
            }
            t.position = destination;

            if (animator != null && !string.IsNullOrEmpty(walkParam)) animator.SetBool(walkParam, false);
        }

        // 기획서 p7: 암전 해제 후 다마고치 강아지를 라인없는 것으로 재배치.
        // Dog_halfside 리그는 본체 파츠 뒤에 "_W" 파츠를 깔아 외곽선을 만든다 → 그 레이어를 끈다.
        IEnumerator DogLineRoutine()
        {
            if (dimmer != null) yield return dimmer.UndimRoutine(dimDuration);

            if (dog != null && !string.IsNullOrEmpty(lineRendererSuffix))
            {
                int changed = 0;
                foreach (var r in dog.GetComponentsInChildren<SpriteRenderer>(true))
                {
                    if (r == null || !r.name.EndsWith(lineRendererSuffix)) continue;
                    r.enabled = false;
                    changed++;
                }
                if (changed == 0)
                    Debug.LogWarning($"[Tutorial] 강아지 아래에 '{lineRendererSuffix}' 로 끝나는 SpriteRenderer가 없습니다.");
            }

            DialogueManager.I?.ResumeFromInteraction();
        }

        // 기획서 p11: 목이 덜렁덜렁한 곰인형을 화면 중앙에 출력.
        IEnumerator BearRoutine()
        {
            if (Require(bearDoll, nameof(bearDoll), "곰인형 출력"))
            {
                bearDoll.SetActive(true);
                if (bearHoldSeconds > 0f) yield return new WaitForSecondsRealtime(bearHoldSeconds);
            }
            DialogueManager.I?.ResumeFromInteraction();
        }

        // 기획서 p14: 맵에 공격 능력 없는 에너미를 배치. 클릭하면 튜토리얼 스테이지로 들어간다.
        IEnumerator EnemyRoutine()
        {
            GameObject spawned = null;
            if (Require(enemyPrefab, nameof(enemyPrefab), "에너미 배치"))
            {
                // 스폰 지점이 없으면 원점(0,0)에 놓여 화면 밖으로 새기 쉬우니 좌표까지 확인한다.
                Vector3 pos = Require(enemySpawnPoint, nameof(enemySpawnPoint), "에너미를 지정 위치에 배치")
                            ? enemySpawnPoint.position : Vector3.zero;
                spawned = Instantiate(enemyPrefab, pos, Quaternion.identity);
                if (spawned.TryGetComponent<MapTutorialEnemy>(out var enemy))
                {
                    Require(enemyNode,  nameof(enemyNode),  "에너미 클릭 시 스테이지 진입");
                    Require(enemyStage, nameof(enemyStage), "에너미 클릭 시 스테이지 진입");
                    enemy.Configure(enemyNode, enemyStage);
                }
                else
                {
                    Debug.LogWarning($"[Tutorial] {enemyPrefab.name} 에 MapTutorialEnemy가 없어 " +
                                     "클릭 진입이 연결되지 않습니다.", this);
                }
            }

            // 기획서 p14: 강아지 집(p4~p5)과 같이 주변을 어둡게 하고 에너미만 남겨 클릭을 유도한다.
            // 스포트라이트 중심은 월드 좌표라 이후 카메라가 좌우로 움직여도 대상에서 어긋나지 않는다.
            // 암전은 에너미를 클릭해 스테이지로 넘어갈 때 씬과 함께 사라진다.
            // 여백·둥글기는 비율이라 에너미 크기가 달라져도 그 에너미에 맞춰 자동으로 잡힌다.
            if (spawned != null && Require(dimmer, nameof(dimmer), "에너미 강조(주변 암전)"))
                yield return dimmer.DimRelativeRoutine(spawned, enemySpotlightPaddingRatio,
                                                       enemySpotlightCornerRatio, dimAmount, dimDuration);

            // 대사는 여기서 끝나지만 에너미를 누를 때까진 다른 것을 누를 수 없다 → 테두리도 에너미만.
            // 에너미가 파괴되면(스테이지 진입) 포커스는 자동으로 풀린다.
            if (spawned != null) MapBusyVisibility.SetHighlightFocus(spawned);

            DialogueManager.I?.ResumeFromInteraction();
        }
    }
}
