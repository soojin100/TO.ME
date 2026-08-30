using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace TOME.Title
{
    /// <summary>UI Image에 스프라이트 시퀀스를 프레임 애니메이션으로 재생.
    /// (To.You 프로젝트의 MainMenuBackgroundAnimator를 TOME 네임스페이스로 이식)</summary>
    [RequireComponent(typeof(Image))]
    public class MainMenuBackgroundAnimator : MonoBehaviour
    {
        [SerializeField] Sprite[] frames;
        [SerializeField] float    fps  = 12f;
        [SerializeField] bool     loop = true;

        Image _image;
        int   _currentIndex;
        Coroutine _routine;

        void Awake()
        {
            _image = GetComponent<Image>();
            if (_image == null)
                Debug.LogError("[MainMenuBackgroundAnimator] Image 컴포넌트가 없습니다.");
            if (frames == null || frames.Length == 0)
                Debug.LogWarning("[MainMenuBackgroundAnimator] frames 배열이 비어 있습니다.");
        }

        void OnEnable()
        {
            if (_image == null || frames == null || frames.Length == 0) return;
            _currentIndex = 0;
            _image.color  = Color.white;
            _image.sprite = frames[0];
            _routine = StartCoroutine(PlayRoutine());
        }

        void OnDisable()
        {
            if (_routine != null) { StopCoroutine(_routine); _routine = null; }
        }

        IEnumerator PlayRoutine()
        {
            float interval = 1f / Mathf.Max(1f, fps);
            var wait = new WaitForSeconds(interval);
            while (loop || _currentIndex < frames.Length)
            {
                _image.sprite = frames[_currentIndex];
                _currentIndex = (_currentIndex + 1) % frames.Length;
                yield return wait;
            }
        }
    }
}
