using System.Collections;
using UnityEngine;
using TOME.Managers;

namespace TOME.Core
{
    public class BootstrapEntry : MonoBehaviour
    {
        [SerializeField] string firstScene = SceneKeys.Map;

        IEnumerator Start()
        {
            // 매니저들은 이 씬에 미리 배치(DontDestroyOnLoad 처리됨)
            // CSV 대사 1회 파싱
            DialogueManager.I?.PreloadAll();
            yield return null;
            yield return SceneLoader.LoadAsync(firstScene);
        }
    }
}
