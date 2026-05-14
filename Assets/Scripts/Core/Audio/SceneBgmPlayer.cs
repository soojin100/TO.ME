using UnityEngine;

namespace TOME.Core
{
    /// <summary>씬 진입 시 지정 BGM 재생.</summary>
    public class SceneBgmPlayer : MonoBehaviour
    {
        [SerializeField] AudioClip clip;

        void Start()
        {
            AudioManager.I?.PlayBgm(clip);
        }
    }
}
