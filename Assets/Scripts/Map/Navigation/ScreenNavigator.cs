using System.Collections;
using UnityEngine;

namespace TOME.Map
{
    /// <summary>좌우 화살표로 카메라를 구역 사이로 부드럽게 이동. To.You RoomGridNavigator 기반 포팅.</summary>
    public class ScreenNavigator : MonoBehaviour
    {
        public static ScreenNavigator Instance { get; private set; }

        public enum Direction { Left, Right }

        [SerializeField] Camera         targetCamera;
        [SerializeField] Transform[]    sections;       // 좌→우 순서의 구역 앵커
        [SerializeField] int            startIndex = 0;
        [SerializeField] float          moveDuration = 0.25f;
        [SerializeField] AnimationCurve moveCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        [SerializeField] GameObject     arrowLeft;
        [SerializeField] GameObject     arrowRight;

        int _index;
        bool _isMoving;
        Coroutine _moveCo;

        public int CurrentIndex => _index;
        public int SectionCount => sections != null ? sections.Length : 0;

        /// <summary>지정 섹션 앵커의 월드 위치. 잘못된 인덱스면 현재 카메라 위치 반환.</summary>
        public Vector3 GetSectionPosition(int index)
        {
            if (sections != null && index >= 0 && index < sections.Length && sections[index] != null)
                return sections[index].position;
            return targetCamera != null ? targetCamera.transform.position : Vector3.zero;
        }

        void Awake()
        {
            Instance = this;
            if (targetCamera == null) targetCamera = Camera.main;
        }

        void OnDestroy()
        {
            // 씬을 떠날 때(스테이지 진입 등) 현재 섹션을 저장 → 복귀 시 그 섹션으로 복원
            if (TOME.Managers.GameManager.I != null)
                TOME.Managers.GameManager.I.SetPendingSectionIndex(_index);
            if (Instance == this) Instance = null;
        }

        void Start()
        {
            // 저장된 섹션이 있으면 그곳으로(스테이지에서 복귀), 없으면 startIndex
            int saved = TOME.Managers.GameManager.I != null ? TOME.Managers.GameManager.I.CurrentSectionIndex : -1;
            int desired = saved >= 0 ? saved : startIndex;
            _index = (sections != null && sections.Length > 0)
                ? Mathf.Clamp(desired, 0, sections.Length - 1) : 0;
            SnapToCurrent();
            RefreshArrows();
        }

        public void MoveLeft()  => TryMove(Direction.Left);
        public void MoveRight() => TryMove(Direction.Right);

        public void TryMove(Direction dir)
        {
            // 대화/컷신 중에는 카메라 이동 금지
            if (TOME.Managers.DialogueManager.I != null && TOME.Managers.DialogueManager.I.IsPlaying) return;
            if (_isMoving || sections == null || sections.Length == 0) return;
            int next = _index + (dir == Direction.Left ? -1 : 1);
            if (next < 0 || next >= sections.Length) return;
            var target = sections[next];
            if (target == null) return;
            _index = next;
            if (_moveCo != null) StopCoroutine(_moveCo);
            _moveCo = StartCoroutine(SmoothMove(target.position));
            RefreshArrows();
        }

        void SnapToCurrent()
        {
            if (targetCamera == null || sections == null || sections.Length == 0) return;
            if (_index < 0 || _index >= sections.Length) return;
            var cell = sections[_index];
            if (cell == null) return;
            var p = targetCamera.transform.position;
            p.x = cell.position.x; p.y = cell.position.y;
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

        void RefreshArrows()
        {
            if (arrowLeft)  arrowLeft.SetActive(_index > 0);
            if (arrowRight) arrowRight.SetActive(sections != null && _index < sections.Length - 1);
        }
    }
}
