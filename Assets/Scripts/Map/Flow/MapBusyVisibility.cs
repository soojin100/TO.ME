using System.Collections.Generic;
using UnityEngine;
using TOME.Managers;
using TOME.Gameplay;

namespace TOME.Map
{
    /// <summary>대화/컷신이 진행되는 동안(=DialogueManager.IsPlaying) 맵의 상호작용 요소를 화면에서 숨긴다.
    /// 숨김 대상: 좌우 화살표(ScreenNavigator), 배회 강아지(CharacterWander), 스테이지/맵 버튼(MapNode·StageNodeButton),
    /// HideDuringDialogue 가 붙은 UI(인벤토리·조합 진입 버튼 등), 그리고 마우스 호버 테두리(SpriteHighlight).
    ///
    /// 테두리는 "지금 누를 수 있다"는 신호라, 누를 수 없는 동안 떠 있으면 안 된다.
    /// SetHighlightFocus로 대상을 하나 지정하면 그 대상만 테두리가 뜬다(튜토리얼의 강아지 집·에너미).
    /// 포커스는 대화 종료와 무관하게 유지되므로, 대사가 끝난 뒤 클릭을 기다리는 구간에도 그대로 쓴다.
    ///
    /// 모든 컷신(Timeline / 인터랙티브 클릭 / CutsceneManager)은 DialogueManager.HandleTrigger를 통해
    /// "대화 재생 중"에 실행되므로 IsPlaying 한 가지 신호로 대화·컷신을 모두 커버한다.
    ///
    /// 씬에 따로 배치/와이어링하지 않아도 되도록 RuntimeInitializeOnLoad로 영속 인스턴스 1개를 자동 생성하고,
    /// 매 프레임 IsPlaying 변화를 감시해 해당 씬의 대상들을 토글한다.</summary>
    public class MapBusyVisibility : MonoBehaviour
    {
        static MapBusyVisibility _inst;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Bootstrap()
        {
            if (_inst != null) return;
            var go = new GameObject("[MapBusyVisibility]");
            _inst = go.AddComponent<MapBusyVisibility>();
            DontDestroyOnLoad(go);
        }

        bool _hidden;
        readonly List<GameObject> _hiddenObjects = new();

        // --- 호버 테두리 ---
        static GameObject _highlightFocus;
        readonly List<Interactables.SpriteHighlight> _blockedHighlights = new();
        GameObject _appliedFocus;
        bool _appliedBusy, _applied;

        /// <summary>테두리를 허용할 단 하나의 대상을 지정한다.
        /// null이면 대화 중엔 전부 차단, 평상시엔 전부 허용으로 돌아간다.
        /// 대상이 파괴되면(씬 전환 등) 자동으로 해제된다.</summary>
        public static void SetHighlightFocus(GameObject focus) => _highlightFocus = focus;

        /// <summary>맵 조작을 막아야 하는 상태인지.
        /// 대사·컷신이 진행 중이거나, "지금은 이것만 누르세요" 포커스가 걸려 있으면 true.
        ///
        /// 포커스를 함께 보는 이유: 튜토리얼 마지막에는 대사가 끝난 뒤에도 에너미를 눌러야 하는 구간이 남는다.
        /// 대사 종료만 보고 맵 UI를 되살리면 그 구간에 화살표와 스테이지 버튼이 튀어나오고,
        /// 아직 잠긴 버튼이라 눌러도 아무 반응이 없어 막힌 것처럼 보인다.</summary>
        public static bool IsBusy
            => (DialogueManager.I != null && DialogueManager.I.IsPlaying) || _highlightFocus != null;

        void OnEnable()  => UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
        void OnDisable() => UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;

        // 씬이 바뀌면 이전 씬의 SpriteHighlight 기록이 전부 무효 → 새 씬 기준으로 다시 적용한다.
        void OnSceneLoaded(UnityEngine.SceneManagement.Scene s, UnityEngine.SceneManagement.LoadSceneMode m)
        {
            _blockedHighlights.Clear();
            _applied = false;
        }

        void Update()
        {
            bool busy = IsBusy;
            if (busy != _hidden)
            {
                if (busy) Hide();
                else      Restore();
            }

            // 파괴된 참조를 진짜 null로 정규화해야 아래 비교가 정확해진다.
            if (_highlightFocus == null) _highlightFocus = null;
            if (_appliedFocus   == null) _appliedFocus   = null;

            // 상태가 바뀔 때만 훑는다 — 매 프레임 FindObjectsByType은 비싸다.
            if (!_applied || busy != _appliedBusy || _highlightFocus != _appliedFocus)
                ApplyHighlights(busy);
        }

        void ApplyHighlights(bool busy)
        {
            foreach (var h in _blockedHighlights)
                if (h != null) h.SetHighlightAllowed(true);
            _blockedHighlights.Clear();

            // 포커스가 있으면 그것만 허용, 없고 대화 중이면 전부 차단, 그 외엔 전부 허용.
            if (_highlightFocus != null || busy)
            {
                foreach (var h in Object.FindObjectsByType<Interactables.SpriteHighlight>(
                             FindObjectsInactive.Include, FindObjectsSortMode.None))
                {
                    if (h == null) continue;
                    if (_highlightFocus != null &&
                        (h.gameObject == _highlightFocus || h.transform.IsChildOf(_highlightFocus.transform)))
                        continue;
                    h.SetHighlightAllowed(false);
                    _blockedHighlights.Add(h);
                }
            }

            _appliedBusy  = busy;
            _appliedFocus = _highlightFocus;
            _applied      = true;
        }

        void Hide()
        {
            _hidden = true;
            _hiddenObjects.Clear();
            CollectAndHide<CharacterWander>();     // 배회 강아지 (Update에서 클릭도 가로채므로 비활성 필요)
            CollectAndHide<MapNode>();             // 맵 노드 버튼
            CollectAndHide<StageNodeButton>();     // 스테이지 버튼
            CollectAndHide<TOME.UI.HideDuringDialogue>();   // 인벤토리·조합 진입 UI 등, 씬에서 표시해 둔 것들
            // 화살표는 ScreenNavigator가 IsPlaying을 보고 스스로 숨긴다.
            ScreenNavigator.Instance?.RefreshArrows();
            ScreenNavigator.Instance?.RefreshSectionButtons();
        }

        void Restore()
        {
            _hidden = false;
            foreach (var go in _hiddenObjects)
                if (go != null) go.SetActive(true);
            _hiddenObjects.Clear();
            // 화살표는 현재 섹션(가장자리 여부)에 맞춰 다시 정리.
            ScreenNavigator.Instance?.RefreshArrows();
            ScreenNavigator.Instance?.RefreshSectionButtons();
        }

        // 현재 활성인 T 컴포넌트들의 GameObject를 비활성화하고, 복원용으로 기록한다.
        void CollectAndHide<T>() where T : Component
        {
            var arr = Object.FindObjectsByType<T>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            foreach (var c in arr)
            {
                if (c == null) continue;
                var go = c.gameObject;
                if (!go.activeSelf) continue;      // 이미 꺼져 있으면 건드리지 않음(복원 시 잘못 켜지 않게)
                go.SetActive(false);
                _hiddenObjects.Add(go);
            }
        }
    }
}
