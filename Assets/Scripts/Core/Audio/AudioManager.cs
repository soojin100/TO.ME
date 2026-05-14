using UnityEngine;

namespace TOME.Core
{
    /// <summary>BGM 1채널 + SFX 1채널. Boot 씬에 배치, DontDestroyOnLoad.</summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager I { get; private set; }

        [SerializeField] AudioSource bgmSource;
        [SerializeField] AudioSource sfxSource;

        [Header("SFX Clips")]
        public AudioClip dogSfx;      // 강아지 대사
        public AudioClip dog2Sfx;     // 조합 성공
        public AudioClip enemySfx;    // 적 사망
        public AudioClip humanSfx;    // 주인 대사

        void Awake()
        {
            if (I != null && I != this) { Destroy(gameObject); return; }
            I = this; DontDestroyOnLoad(gameObject);
        }

        public void PlayBgm(AudioClip clip)
        {
            if (!bgmSource || !clip) return;
            if (bgmSource.clip == clip && bgmSource.isPlaying) return;
            bgmSource.clip = clip;
            bgmSource.loop = true;
            bgmSource.Play();
        }

        public void StopBgm()
        {
            if (bgmSource) bgmSource.Stop();
        }

        public void PlaySfx(AudioClip clip)
        {
            if (!sfxSource || !clip) return;
            sfxSource.PlayOneShot(clip);
        }
    }
}
