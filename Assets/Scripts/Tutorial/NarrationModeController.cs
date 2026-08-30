using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using TOME.Core;
namespace TOME.Tutorial
{
    /// <summary>나레이션 구간 동안 화면을 균일하게 어둡게 하고 맵 오브젝트의 콜라이더를 꺼
    /// 대화창·스킵 버튼만 동작하게 한다 (기획서 p7).
    ///
    /// "투명화"를 알파 0(완전 소멸)이 아니라 반투명 암전으로 구현한 이유: 기획서 목업
    /// p7 우측~p13이 6장 연속으로 방이 어둡게 보이는 상태를 그리고 있고, p8에는 배회 강아지도
    /// 그대로 보인다. 같은 문장의 "스탠딩 캐릭터 아무것도 출력되지 않고"는 큰 초상화 얘기이며
    /// (p9부터 등장) dialogue 시트의 standing 컬럼을 비워 두는 것으로 충족된다.
    ///
    /// SetActive(false)를 쓰지 않는 이유: MapBusyVisibility가 이미 대화 중 SetActive를 토글하고 있어
    /// 두 시스템이 같은 오브젝트를 켜고 끄면 복원 상태가 꼬인다. 암전·콜라이더만 만지면 겹쳐도 안전하다.</summary>
    public class NarrationModeController : MonoBehaviour
    {
        [Tooltip("암전을 담당할 SpotlightDimmer. 스포트라이트 없이(=균일) 사용한다.")]
        [SerializeField] SpotlightDimmer dimmer;
        [Tooltip("콜라이더를 끌 범위. 보통 맵 배경 루트(예: Room_Background). 비우면 이 오브젝트 기준.")]
        [SerializeField] Transform contentRoot;
        [Tooltip("입력을 살려 둘 오브젝트(자식 포함).")]
        [SerializeField] GameObject[] keepInteractive;
        [Range(0f, 1f)]
        [Tooltip("나레이션 중 화면 어둡기. 0=그대로, 1=완전 암전.")]
        [SerializeField] float dimAmount = 0.55f;
        [SerializeField] float fadeDuration = 0.35f;

        readonly List<Collider2D> _disabled = new(32);
        bool _active;

        public bool IsActive => _active;

        /// <summary>나레이션 모드 진입. 스텝이 이 코루틴을 yield 해 완료를 기다린다.</summary>
        public IEnumerator EnterRoutine()
        {
            if (_active) yield break;
            _active = true;

            // 콜라이더를 먼저 꺼서 페이드 도중 클릭이 새지 않게 한다.
            CollectAndDisableColliders();

            if (dimmer != null)
                yield return dimmer.DimRoutine(null, 0f, 0f, dimAmount, fadeDuration);
        }

        /// <summary>나레이션 모드 종료. 암전과 콜라이더를 원복한다.</summary>
        public IEnumerator ExitRoutine()
        {
            if (!_active) yield break;
            if (dimmer != null) yield return dimmer.UndimRoutine(fadeDuration);
            Restore();
        }

        void OnDisable() { if (_active) Restore(); }

        void CollectAndDisableColliders()
        {
            _disabled.Clear();
            var root = contentRoot != null ? contentRoot : transform;

            foreach (var c in root.GetComponentsInChildren<Collider2D>(true))
            {
                if (c == null || !c.enabled || IsKept(c.transform)) continue;
                c.enabled = false;
                _disabled.Add(c);
            }
        }

        bool IsKept(Transform t)
        {
            if (keepInteractive == null) return false;
            foreach (var go in keepInteractive)
            {
                if (go == null) continue;
                if (t == go.transform || t.IsChildOf(go.transform)) return true;
            }
            return false;
        }

        void Restore()
        {
            foreach (var c in _disabled)
                if (c != null) c.enabled = true;
            _disabled.Clear();
            _active = false;
        }
    }
}
