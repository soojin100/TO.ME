using UnityEngine;

namespace TOME.Core
{
    /// <summary>영속 매니저 묶음의 루트. 중복 생성을 막고 씬 전환에도 유지한다.
    /// (자식 매니저들은 이 루트가 DontDestroyOnLoad라 함께 유지됨)</summary>
    public class PersistentManagersRoot : MonoBehaviour
    {
        static PersistentManagersRoot _instance;

        void Awake()
        {
            if (_instance != null && _instance != this) { Destroy(gameObject); return; }
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        void OnDestroy() { if (_instance == this) _instance = null; }
    }
}
