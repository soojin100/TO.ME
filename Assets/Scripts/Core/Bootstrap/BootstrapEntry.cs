using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
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

            // 전환 코루틴은 영속 SceneFader가 호스팅한다.
            // (Boot 씬이 언로드되면 이 BootstrapEntry는 파괴되므로 직접 호스팅하면 안 됨)
            if (SceneFader.I != null)
                SceneFader.I.TransitionToScene(firstScene);
            else
                SceneManager.LoadSceneAsync(firstScene); // 폴백: 페이더 없으면 직접 로드
        }
    }
}
