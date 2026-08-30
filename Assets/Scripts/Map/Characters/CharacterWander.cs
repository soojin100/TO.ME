using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using TOME.Characters;
using TOME.Combat;
namespace TOME.Map
{
    public class CharacterWander : MonoBehaviour
    {
        [Header("이동 영역")]
        [SerializeField] float minX = -3f;
        [SerializeField] float maxX = 3f;
        [SerializeField] float minY = -5f;
        [SerializeField] float maxY = -3f;

        [Header("이동 설정")]
        [SerializeField] float moveSpeed = 2f;
        [SerializeField] float minIdleTime = 1f;
        [SerializeField] float maxIdleTime = 3f;

        [Header("기본 애니메이션 파라미터")]
        [SerializeField] string walkParam = "walk";

        [Header("등록 애니메이션 목록")]
        [SerializeField] List<CharacterAnimationSO> animations;

        [Header("클릭/터치 시 재생")]
        [SerializeField] string onClickAnimationId = "jump";

        [Header("드래그 설정")]
        [SerializeField] string dragAnimationId = "climbing";
        [SerializeField] float dragThreshold = 0.1f;
        [SerializeField] float dropSpeed = 3f;

        [Header("배회 시작 제어")]
        [Tooltip("false면 Start에서 배회하지 않는다. 튜토리얼 소환 연출이 끝난 뒤 BeginWander()로 시작(기획서 p5).")]
        [SerializeField] bool autoStartWander = true;

        [SerializeField] Animator animator;

        bool _clickRequested;
        bool _isDragging;
        Vector3 _dragOffset;
        Camera _cam;
        Vector3 _originalScale;  // 좌우 반전 시 부호만 바꾸기 위한 원본 스케일
        float _originalY;        // 드래그 후 되돌아갈 바닥 높이

        readonly Dictionary<string, CharacterAnimationSO> _animTable = new();
        bool _started;   // Start 1회 초기화 완료 여부 (재활성화 시 배회 재개 판단)
        bool _paused;    // 스테이지 정보창이 떠 있는 동안 true — 이동·클릭 반응 정지, 제자리 idle만

        public void PlayAnimation(string id) => StartCoroutine(PlayAnimRoutine(id));

        /// <summary>배회·입력 반응을 멈추고 제자리 idle 애니메이션만 남긴다(기획서: 스테이지 정보창 동안).
        /// 해제하면 배회를 재개한다.</summary>
        public void SetPaused(bool paused)
        {
            if (_paused == paused) return;
            _paused = paused;
            if (paused)
            {
                StopAllCoroutines();
                _isDragging = false;
                SetWalk(false);
                // 드래그·등반 중이었다면 유지형 Bool을 내려 idle로 되돌린다.
                if (_animTable.TryGetValue(dragAnimationId, out var dragAnim))
                    animator.SetBool(dragAnim.animatorParamName, false);
            }
            else if (_started && isActiveAndEnabled && autoStartWander)
            {
                StartCoroutine(WanderRoutine());
            }
        }

        /// <summary>튜토리얼 소환 연출이 끝난 뒤 배회를 시작한다. 이미 배회 중이면 무시.
        /// 이후 대화 중 SetActive 토글로 숨겼다 켜도 배회가 재개된다(autoStartWander가 켜지므로).</summary>
        public void BeginWander()
        {
            if (autoStartWander) return;
            autoStartWander = true;
            if (_started && isActiveAndEnabled) StartCoroutine(WanderRoutine());
        }

        void Awake()
        {
            _originalScale = transform.localScale;
            foreach (var a in animations)
                if (a != null) _animTable[a.animationId] = a;
        }

        void Start()
        {
            _cam = Camera.main;
            _originalY = transform.position.y;

            var core = GetComponent<CharacterCore>();
            if (core) core.RebindOnly();

            _started = true;
            if (autoStartWander) StartCoroutine(WanderRoutine());
        }

        // 대화/컷신 동안 MapBusyVisibility가 SetActive(false)로 숨겼다가 다시 켜면 Start가 재호출되지 않으므로
        // 여기서 배회를 재개한다. (비활성화 시 코루틴은 Unity가 자동 정지)
        void OnEnable()
        {
            if (_started && autoStartWander && !_paused) StartCoroutine(WanderRoutine());
        }

        void Update()
        {
            if (_paused) return;   // 정보창 동안 클릭/드래그 반응 정지
            if (_cam == null) _cam = Camera.main;   // 씬 전환 직후 등 캐시가 빌 수 있다
            if (_cam == null) return;

            if (Input.GetMouseButtonDown(0))
            {
                var worldPos = _cam.ScreenToWorldPoint(Input.mousePosition);
                worldPos.z = 0f;

                var hit = Physics2D.OverlapPoint(worldPos);
                if (hit != null && hit.gameObject == gameObject)
                {
                    _isDragging = true;
                    _dragOffset = transform.position - worldPos;
                    StopAllCoroutines();
                    SetWalk(false);
                    // 드래그 동안 유지되는 Bool은 PlayAnimRoutine(1회 재생 후 해제)이 아니라
                    // mouse-up의 SetBool(false)와 짝으로 직접 켠다.
                    if (_animTable.TryGetValue(dragAnimationId, out var dragAnim))
                        animator.SetBool(dragAnim.animatorParamName, true);
                }
                else _clickRequested = true;
            }

            if (Input.GetMouseButton(0) && _isDragging)
            {
                var worldPos = _cam.ScreenToWorldPoint(Input.mousePosition);
                worldPos.z = 0f;
                transform.position = worldPos + _dragOffset;
            }

            if (Input.GetMouseButtonUp(0) && _isDragging)
            {
                _isDragging = false;
                StartCoroutine(DropToOriginalY());
            }
        }

        IEnumerator DropToOriginalY()
        {
            // 내려놓는 동안 climbing 유지 → 착지 후 해제하고 배회 재개
            while (Mathf.Abs(transform.position.y - _originalY) > 0.01f)
            {
                var pos = transform.position;
                pos.y = Mathf.MoveTowards(pos.y, _originalY, dropSpeed * Time.deltaTime);
                transform.position = pos;
                yield return null;
            }
            var finalPos = transform.position;
            finalPos.y = _originalY;
            transform.position = finalPos;

            if (_animTable.TryGetValue(dragAnimationId, out var anim))
                animator.SetBool(anim.animatorParamName, false);
            StartCoroutine(WanderRoutine());
        }

        IEnumerator WanderRoutine()
        {
            while (true)
            {
                if (_clickRequested)
                {
                    _clickRequested = false;
                    yield return PlayAnimRoutine(onClickAnimationId);
                    continue;
                }

                SetWalk(false);
                float elapsed = 0f;
                float idleTime = Random.Range(minIdleTime, maxIdleTime);
                while (elapsed < idleTime)
                {
                    if (_clickRequested) break;
                    elapsed += Time.deltaTime;
                    yield return null;
                }
                if (_clickRequested) continue;

                var target = new Vector3(
                    Random.Range(minX, maxX),
                    Random.Range(minY, maxY), 0f);

                SetWalk(true);
                float dir = target.x - transform.position.x;
                if (Mathf.Abs(dir) > 0.01f)
                {
                    float sign = -Mathf.Sign(dir);
                    transform.localScale = new Vector3(
                        Mathf.Abs(_originalScale.x) * sign,
                        _originalScale.y,
                        _originalScale.z);
                }

                while (Vector3.Distance(transform.position, target) > 0.05f)
                {
                    if (_clickRequested) break;
                    transform.position = Vector3.MoveTowards(
                        transform.position, target, moveSpeed * Time.deltaTime);
                    yield return null;
                }
            }
        }

        IEnumerator PlayAnimRoutine(string id)
        {
            if (!_animTable.TryGetValue(id, out var anim)) yield break;

            SetWalk(false);

            if (anim.paramType == CharacterAnimationSO.AnimParamType.Trigger)
                animator.SetTrigger(anim.animatorParamName);
            else
                animator.SetBool(anim.animatorParamName, true);

            if (anim.waitForEnd)
            {
                yield return null;
                while (animator.GetCurrentAnimatorStateInfo(0).normalizedTime < 1f
                       && !animator.IsInTransition(0))
                    yield return null;
            }

            if (anim.paramType == CharacterAnimationSO.AnimParamType.Bool)
                animator.SetBool(anim.animatorParamName, false);

        }

        void SetWalk(bool walking)
        {
            animator.SetBool(walkParam, walking);
        }
    }
}