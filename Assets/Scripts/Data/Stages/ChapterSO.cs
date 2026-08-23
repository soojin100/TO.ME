using UnityEngine;

namespace TOME.Data
{
    /// <summary>챕터(=맵) 단위 테마/적 로스터. 챕터가 바뀌면 배경색·등장 적·BGM이 통째로 달라진다.</summary>
    [CreateAssetMenu(menuName = "TOME/Chapter", fileName = "Chapter_")]
    public class ChapterSO : ScriptableObject
    {
        public string id;
        public string title;

        [Header("Theme")]
        public Color     backgroundColor = new Color(0.96f, 0.80f, 0.80f, 1f); // 카메라 배경색(미사용, 호환용)
        public Sprite    backgroundSprite;   // (호환용 폴백, 단일 스프라이트만 쓸 때)
        [Tooltip("이 챕터의 맵 World prefab. Stage 씬에 instantiate되어 반투명 배경으로 표시됨. ScrollSections/Items 자식은 자동 비활성화.")]
        public GameObject backgroundPrefab;
        public AudioClip bgm;                 // (선택) 챕터 BGM
        public string    mapSceneName;        // 이 챕터의 맵 씬 이름 (비면 기본 맵)

        [Header("Progression")]
        [Tooltip("이 챕터의 마지막 스테이지(보스) 노드. 이걸 클리어하면 nextChapter로 넘어간다.")]
        public NodeSO    finalNode;
        [Tooltip("보스 클리어 후 이동할 다음 챕터. 비어 있으면 마지막 챕터.")]
        public ChapterSO nextChapter;

        [Header("Enemies")]
        [Tooltip("이 챕터 스테이지의 자동 스폰 적 풀. StageSO.spawns가 비면 difficulty와 함께 사용.")]
        public EnemySO[] enemyRoster;
    }
}
