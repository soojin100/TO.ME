using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TOME.Map
{
    /// <summary>좌우 화살표로 카메라를 구역 사이로 부드럽게 이동. To.You RoomGridNavigator 기반 포팅.
    /// sections를 비워두면 맵 전체 폭과 실제 화면 종횡비로 구역을 런타임에 계산한다
    /// (세로 모바일은 기기마다 보이는 폭이 달라서, 앵커를 씬에 박아두면 맵이 건너뛰어진다).</summary>
    public class ScreenNavigator : MonoBehaviour
    {
        public static ScreenNavigator Instance { get; private set; }

        public enum Direction { Left, Right }

        [SerializeField] Camera         targetCamera;
        [Tooltip("좌→우 순서의 구역 앵커. 비워두면 mapRoot 크기와 화면 종횡비로 자동 계산한다.")]
        [SerializeField] Transform[]    sections;
        [Tooltip("자동 계산에 쓸 맵 루트. 비우면 이름이 _Background로 끝나는 루트를 찾고, 없으면 씬의 모든 SpriteRenderer를 감싼다.")]
        [SerializeField] Transform      mapRoot;
        [Tooltip("최소 구역 수. 스테이지 버튼을 구역마다 하나씩 두려면 버튼 개수만큼 잡는다. " +
                 "화면 폭으로 계산한 값보다 크면 이 값을 쓴다 — 구역이 많을수록 한 번에 덜 움직여 맵이 건너뛰어지지 않는다.")]
        [SerializeField] int            minSections = 0;
        [SerializeField] int            startIndex = 0;
        [SerializeField] float          moveDuration = 0.25f;
        [SerializeField] AnimationCurve moveCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        [SerializeField] GameObject     arrowLeft;
        [SerializeField] GameObject     arrowRight;

        Vector3[] _anchors = Array.Empty<Vector3>();
        int  _index;
        bool _isMoving;
        Coroutine _moveCo;
        int _builtForWidth, _builtForHeight;

        public int CurrentIndex => _index;
        public int SectionCount => _anchors.Length;

        /// <summary>지정 구역 앵커의 월드 위치. 잘못된 인덱스면 현재 카메라 위치 반환.</summary>
        public Vector3 GetSectionPosition(int index)
        {
            if (index >= 0 && index < _anchors.Length) return _anchors[index];
            return targetCamera != null ? targetCamera.transform.position : Vector3.zero;
        }

        /// <summary>월드 X가 속한 구역 인덱스. 스테이지 버튼을 맵 위 위치에 두고 구역을 역산할 때 사용
        /// — 구역 개수가 기기마다 달라져도 안 깨진다.</summary>
        public int SectionIndexAtWorldX(float worldX)
        {
            if (_anchors.Length == 0) return 0;
            int best = 0;
            float bestDist = Mathf.Abs(_anchors[0].x - worldX);
            for (int i = 1; i < _anchors.Length; i++)
            {
                float d = Mathf.Abs(_anchors[i].x - worldX);
                if (d < bestDist) { bestDist = d; best = i; }
            }
            return best;
        }

        void Awake()
        {
            Instance = this;
            if (targetCamera == null) targetCamera = Camera.main;
        }

        void OnDestroy()
        {
            // 씬을 떠날 때(스테이지 진입 등) 현재 구역을 저장 → 복귀 시 그 구역으로 복원
            if (TOME.Managers.GameManager.I != null)
                TOME.Managers.GameManager.I.SetPendingSectionIndex(_index);
            if (Instance == this) Instance = null;
        }

        void Start()
        {
            BuildAnchors();

            // 저장된 구역이 있으면 그곳으로(스테이지에서 복귀), 없으면 startIndex
            int saved = TOME.Managers.GameManager.I != null ? TOME.Managers.GameManager.I.CurrentSectionIndex : -1;
            int desired = saved >= 0 ? saved : startIndex;
            _index = _anchors.Length > 0 ? Mathf.Clamp(desired, 0, _anchors.Length - 1) : 0;
            SnapToCurrent();
            RefreshArrows();
        }

        void Update()
        {
            // 화면 크기/방향이 바뀌면 구역 재계산 (기기 회전, 에디터 Game뷰 리사이즈)
            if (Screen.width == _builtForWidth && Screen.height == _builtForHeight) return;
            float prevX = _anchors.Length > 0 ? _anchors[Mathf.Clamp(_index, 0, _anchors.Length - 1)].x : 0f;
            BuildAnchors();
            _index = _anchors.Length > 0 ? SectionIndexAtWorldX(prevX) : 0;
            if (!_isMoving) SnapToCurrent();
            RefreshArrows();
        }

        /// <summary>구역 앵커 생성. sections가 지정돼 있으면 그 위치를 쓰고,
        /// 비어 있으면 맵 폭 ÷ 화면에 보이는 폭으로 개수를 정해 균등 분할한다.</summary>
        void BuildAnchors()
        {
            _builtForWidth  = Screen.width;
            _builtForHeight = Screen.height;

            if (sections != null && sections.Length > 0)
            {
                var list = new List<Vector3>(sections.Length);
                foreach (var t in sections) if (t != null) list.Add(t.position);
                _anchors = list.ToArray();
                return;
            }

            if (targetCamera == null || !targetCamera.orthographic || !TryGetMapBounds(out Bounds b))
            {
                _anchors = targetCamera != null
                    ? new[] { targetCamera.transform.position }
                    : Array.Empty<Vector3>();
                return;
            }

            float halfW = targetCamera.orthographicSize * targetCamera.aspect;   // 화면 반폭(월드)
            float minX  = b.min.x + halfW;   // 카메라가 갈 수 있는 최좌단
            float maxX  = b.max.x - halfW;   // 최우단
            float y     = b.center.y;

            if (maxX <= minX)                // 맵이 화면보다 좁으면 구역 1개
            {
                _anchors = new[] { new Vector3(b.center.x, y, 0f) };
                return;
            }

            // 화면 폭 기준 개수가 하한이다(그보다 적으면 맵이 건너뛰어진다). 구역이 더 많은 건 안전하다.
            int byScreen = Mathf.CeilToInt(b.size.x / (halfW * 2f));
            int count = Mathf.Max(2, Mathf.Max(minSections, byScreen));
            var arr = new Vector3[count];
            for (int i = 0; i < count; i++)
                arr[i] = new Vector3(Mathf.Lerp(minX, maxX, i / (float)(count - 1)), y, 0f);
            _anchors = arr;
        }

        bool TryGetMapBounds(out Bounds bounds)
        {
            bounds = default;

            Transform root = mapRoot;
            if (root == null)
            {
                foreach (var go in gameObject.scene.GetRootGameObjects())
                    if (go.name.EndsWith("_Background", StringComparison.Ordinal)) { root = go.transform; break; }
            }

            var renderers = root != null
                ? root.GetComponentsInChildren<SpriteRenderer>(true)
                : FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include);

            bool any = false;
            foreach (var r in renderers)
            {
                if (r == null || r.sprite == null) continue;
                if (!any) { bounds = r.bounds; any = true; }
                else bounds.Encapsulate(r.bounds);
            }
            return any;
        }

        public void MoveLeft()  => TryMove(Direction.Left);
        public void MoveRight() => TryMove(Direction.Right);

        /// <summary>지정 구역으로 부드럽게 이동. 컷신 등 외부 시스템이 호출. 이미 같은 구역이면 null 반환.
        /// 반환 Coroutine을 yield하면 이동 완료까지 대기 가능.</summary>
        public Coroutine MoveToSection(int idx)
        {
            if (idx < 0 || idx >= _anchors.Length) return null;
            if (idx == _index) return null;
            _index = idx;
            if (_moveCo != null) StopCoroutine(_moveCo);
            _moveCo = StartCoroutine(SmoothMove(_anchors[idx]));
            RefreshArrows();
            return _moveCo;
        }

        public void TryMove(Direction dir)
        {
            // 대화/컷신 중에는 카메라 이동 금지
            if (TOME.Managers.DialogueManager.I != null && TOME.Managers.DialogueManager.I.IsPlaying) return;
            if (_isMoving || _anchors.Length == 0) return;
            int next = _index + (dir == Direction.Left ? -1 : 1);
            if (next < 0 || next >= _anchors.Length) return;
            _index = next;
            if (_moveCo != null) StopCoroutine(_moveCo);
            _moveCo = StartCoroutine(SmoothMove(_anchors[next]));
            RefreshArrows();
        }

        void SnapToCurrent()
        {
            if (targetCamera == null || _index < 0 || _index >= _anchors.Length) return;
            var cell = _anchors[_index];
            var p = targetCamera.transform.position;
            p.x = cell.x; p.y = cell.y;
            targetCamera.transform.position = p;
        }

        IEnumerator SmoothMove(Vector3 targetPos)
        {
            _isMoving = true;
            Vector3 start = targetCamera.transform.position;
            Vector3 end = new(targetPos.x, targetPos.y, start.z);
            float t = 0f;
            while (t < moveDuration)
            {
                t += Time.unscaledDeltaTime;
                targetCamera.transform.position =
                    Vector3.Lerp(start, end, moveCurve.Evaluate(t / moveDuration));
                yield return null;
            }
            targetCamera.transform.position = end;
            _isMoving = false;
        }

        /// <summary>좌우 화살표 표시 갱신. 대화/컷신(DialogueManager.IsPlaying) 중에는 둘 다 숨김.
        /// MapBusyVisibility가 대화 시작/종료 시 호출해 즉시 반영한다.</summary>
        public void RefreshArrows()
        {
            bool busy = TOME.Managers.DialogueManager.I != null && TOME.Managers.DialogueManager.I.IsPlaying;
            if (arrowLeft)  arrowLeft.SetActive(!busy && _index > 0);
            if (arrowRight) arrowRight.SetActive(!busy && _index < _anchors.Length - 1);
        }
    }
}
