using System.Collections.Generic;
using UnityEngine;
using TOME.Managers;
using TOME.Gameplay;

namespace TOME.Map
{
    /// <summary>대화/컷신이 진행되는 동안(=DialogueManager.IsPlaying) 맵의 상호작용 요소를 화면에서 숨긴다.
    /// 숨김 대상: 좌우 화살표(ScreenNavigator), 배회 강아지(CharacterWander), 스테이지/맵 버튼(MapNode·StageNodeButton).
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

        void Update()
        {
            bool busy = DialogueManager.I != null && DialogueManager.I.IsPlaying;
            if (busy == _hidden) return;          // 상태 변화 없음
            if (busy) Hide();
            else      Restore();
        }

        void Hide()
        {
            _hidden = true;
            _hiddenObjects.Clear();
            CollectAndHide<CharacterWander>();     // 배회 강아지 (Update에서 클릭도 가로채므로 비활성 필요)
            CollectAndHide<MapNode>();             // 맵 노드 버튼
            CollectAndHide<StageNodeButton>();     // 스테이지 버튼
            // 화살표는 ScreenNavigator가 IsPlaying을 보고 스스로 숨긴다.
            ScreenNavigator.Instance?.RefreshArrows();
        }

        void Restore()
        {
            _hidden = false;
            foreach (var go in _hiddenObjects)
                if (go != null) go.SetActive(true);
            _hiddenObjects.Clear();
            // 화살표는 현재 섹션(가장자리 여부)에 맞춰 다시 정리.
            ScreenNavigator.Instance?.RefreshArrows();
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
