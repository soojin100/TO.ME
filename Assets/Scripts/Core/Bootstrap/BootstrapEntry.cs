using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using TOME.Managers;

namespace TOME.Core
{
    /// <summary>타이틀(TOUCH TO START) 화면. 탭하면 첫 씬으로 전환.
    /// waitForTouch=false면 종전처럼 즉시 전환(테스트용).</summary>
    public class BootstrapEntry : MonoBehaviour
    {
        [SerializeField] string firstScene = SceneKeys.Map;
        [SerializeField] bool   waitForTouch = true;   // 타이틀에서 탭 대기
        [SerializeField] GameObject touchToStartHint;  // "TOUCH TO START" 라벨(선택)

        bool _transitioning;

        IEnumerator Start()
        {
            // 매니저들은 이 씬에 미리 배치(DontDestroyOnLoad 처리됨). CSV 대사 1회 파싱.
            DialogueManager.I?.PreloadAll();
            yield return null;

            if (!waitForTouch) { Go(); yield break; }

            if (touchToStartHint) touchToStartHint.SetActive(true);
            // 첫 탭/클릭까지 타이틀(애니메이션 배경) 유지
            while (!_transitioning)
            {
                if (Input.GetMouseButtonDown(0) || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began))
                    Go();
                yield return null;
            }
        }

        void Go()
        {
            if (_transitioning) return;
            _transitioning = true;
            if (touchToStartHint) touchToStartHint.SetActive(false);

            // 전환 코루틴은 영속 SceneFader가 호스팅한다.
            // (Boot 씬이 언로드되면 이 BootstrapEntry는 파괴되므로 직접 호스팅하면 안 됨)
            if (SceneFader.I != null)
                SceneFader.I.TransitionToScene(firstScene);
            else
                SceneManager.LoadSceneAsync(firstScene); // 폴백: 페이더 없으면 직접 로드
        }
    }
}
