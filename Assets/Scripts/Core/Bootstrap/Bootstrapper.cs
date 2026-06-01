using UnityEngine;
using TOME.Managers;

namespace TOME.Core
{
    /// <summary>어느 씬에서 Play를 시작하든 영속 매니저(GameManager/SceneFader/DialogueManager/
    /// SaveSystemManager/MapManager)가 존재하도록 보장한다. Boot를 거쳐 시작하면 이미 있으므로
    /// 생성하지 않는다(중복 방지). → Room_Hallway/Stage 등 아무 씬에서 바로 Play 가능.</summary>
    public static class Bootstrapper
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void EnsureManagers()
        {
            if (GameManager.I != null) return;   // 이미 매니저 존재(Boot 경유 등)

            var prefab = Resources.Load<GameObject>("Bootstrap/PersistentManagers");
            if (prefab == null)
            {
                Debug.LogWarning("[Bootstrapper] Resources/Bootstrap/PersistentManagers.prefab 를 찾을 수 없습니다.");
                return;
            }
            var go = Object.Instantiate(prefab);
            go.name = "PersistentManagers";
        }
    }
}
