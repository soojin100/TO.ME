using UnityEngine;
using UnityEngine.SceneManagement;

namespace TOME.Core
{
    /// <summary>BGM 1채널 + SFX 1채널. Boot 씬에 배치, DontDestroyOnLoad.
    /// 씬 로드 시 다른 loop AudioSource가 있으면 정지(중복 BGM 방지).</summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager I { get; private set; }

        [SerializeField] AudioSource bgmSource;
        [SerializeField] AudioSource sfxSource;
        [Tooltip("씬 로드 시 bgmSource/sfxSource 외 다른 loop AudioSource를 강제 정지. 의도된 ambient가 있으면 끄세요.")]
        [SerializeField] bool stopRogueLoopAudio = true;

        [Header("SFX Clips")]
        public AudioClip dogSfx;      // 강아지 대사
        public AudioClip dog2Sfx;     // 조합 성공
        public AudioClip enemySfx;    // 적 사망
        public AudioClip humanSfx;    // 주인 대사

        void Awake()
        {
            if (I != null && I != this) { Destroy(gameObject); return; }
            I = this; DontDestroyOnLoad(gameObject);

            // 안전벨트: 인스펙터 실수로 잘못 설정돼도 BGM/SFX 역할 분리 보장
            if (bgmSource)
            {
                bgmSource.playOnAwake = false;
                bgmSource.loop = true;
            }
            if (sfxSource)
            {
                sfxSource.playOnAwake = false;
                sfxSource.loop = false;
            }

            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            if (I == this) I = null;
        }

        public void PlayBgm(AudioClip clip)
        {
            if (!bgmSource) return;
            if (clip == null) { bgmSource.Stop(); bgmSource.clip = null; return; }
            if (bgmSource.clip == clip && bgmSource.isPlaying) return;
            bgmSource.Stop();
            bgmSource.clip = clip;
            bgmSource.loop = true;
            bgmSource.playOnAwake = false;
            bgmSource.Play();
        }

        public void StopBgm()
        {
            if (bgmSource) bgmSource.Stop();
        }

        public void PlaySfx(AudioClip clip)
        {
            if (!sfxSource || !clip) return;
            sfxSource.loop = false;                 // 효과음은 절대 반복 X
            sfxSource.PlayOneShot(clip);
        }

        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (!stopRogueLoopAudio) return;
            // bgmSource/sfxSource 외에 loop=true로 재생 중인 AudioSource는 BGM 중복으로 간주 → 정지
            var all = FindObjectsByType<AudioSource>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            foreach (var a in all)
            {
                if (a == null) continue;
                if (a == bgmSource || a == sfxSource) continue;
                if (a.loop && a.isPlaying)
                {
                    Debug.LogWarning($"[AudioManager] Rogue loop AudioSource stopped: '{a.gameObject.name}' clip='{(a.clip ? a.clip.name : "<null>")}'. Route BGM through AudioManager.PlayBgm.");
                    a.Stop();
                }
            }
        }
    }
}
