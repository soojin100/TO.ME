using System.Collections;
using System.Collections.Generic;
using TOME.Data;
using TOME.Gameplay.Player;
using UnityEngine;

namespace TOME.Gameplay
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

        [SerializeField] Animator animator;

        public void PlayAnimation(string id) => StartCoroutine(PlayAnimRoutine(id));

        enum WanderState { Idle, Walk, Busy }
        WanderState _state = WanderState.Idle;
        bool _clickRequested;
        bool _isDragging;
        Vector3 _dragOffset;
        Camera _cam;
        Vector3 _originalScale;  // ← 추가
        float _originalY;        // ← 추가

        readonly Dictionary<string, CharacterAnimationSO> _animTable = new();
        bool _started;   // Start 1회 초기화 완료 여부 (재활성화 시 배회 재개 판단)

        void Awake()
        {
            _originalScale = transform.localScale;  // ← 추가
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
            StartCoroutine(WanderRoutine());
        }

        // 대화/컷신 동안 MapBusyVisibility가 SetActive(false)로 숨겼다가 다시 켜면 Start가 재호출되지 않으므로
        // 여기서 배회를 재개한다. (비활성화 시 코루틴은 Unity가 자동 정지)
        void OnEnable()
        {
            if (_started) StartCoroutine(WanderRoutine());
        }

        void Update()
        {
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

            if (anim.blockWander) _state = WanderState.Busy;
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

            _state = WanderState.Idle;
        }

        void SetWalk(bool walking)
        {
            animator.SetBool(walkParam, walking);
        }
    }
}