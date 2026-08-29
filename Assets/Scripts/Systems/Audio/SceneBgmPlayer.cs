using UnityEngine;

namespace TOME.Systems
{
    /// <summary>씬 전용 BGM. 이 오브젝트의 AudioSource로 직접 재생한다.
    /// 씬이 내려가면 이 오브젝트(AudioSource 포함)가 함께 파괴되어 BGM도 자동으로 멈춘다
    /// → 씬 간 BGM 겹침/잔향이 구조적으로 발생하지 않는다.</summary>
    public class SceneBgmPlayer : MonoBehaviour
    {
        [SerializeField] AudioClip clip;
        [Range(0f, 1f)][SerializeField] float volume = 0.5f;
        [Tooltip("켜면 현재 스테이지(GameManager.CurrentStage)의 bgm이 있을 때 그걸 우선 재생. " +
                 "Stage 씬처럼 한 씬을 여러 스테이지가 공유할 때 보스 등 스테이지별 BGM용. clip은 기본값(폴백).")]
        [SerializeField] bool useCurrentStageBgm = false;
        [Tooltip("켜면 현재 챕터(GameManager.CurrentChapter)의 bgm이 있을 때 그걸 우선 재생. " +
                 "맵 씬(Room_*)이 같은 구조를 공유하고 챕터별로 BGM만 다를 때 사용. clip은 기본값(폴백).")]
        [SerializeField] bool useCurrentChapterBgm = false;

        void Start()
        {
            AudioClip toPlay = clip;
            if (useCurrentStageBgm)
            {
                var stage = GameManager.I != null ? GameManager.I.CurrentStage : null;
                if (stage != null && stage.bgm != null) toPlay = stage.bgm;   // 스테이지 전용 BGM 우선
            }
            else if (useCurrentChapterBgm)
            {
                var chapter = GameManager.I != null ? GameManager.I.CurrentChapter : null;
                if (chapter != null && chapter.bgm != null) toPlay = chapter.bgm;   // 챕터 전용 BGM 우선
            }

            var src = GetComponent<AudioSource>();
            if (!src) src = gameObject.AddComponent<AudioSource>();
            src.clip          = toPlay;
            src.loop          = true;
            src.playOnAwake   = false;
            src.volume        = volume;
            src.spatialBlend  = 0f;     // 2D (거리 감쇠 없음)
            if (toPlay) src.Play();
        }
    }
}
