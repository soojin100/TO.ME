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
        public Color     backgroundColor = new Color(0.96f, 0.80f, 0.80f, 1f); // 카메라 배경색
        public Sprite    backgroundSprite;   // (선택) 배경 스프라이트
        public AudioClip bgm;                 // (선택) 챕터 BGM
        public string    mapSceneName;        // 이 챕터의 맵 씬 이름 (비면 기본 맵)

        [Header("Enemies")]
        [Tooltip("이 챕터 스테이지의 자동 스폰 적 풀. StageSO.spawns가 비면 difficulty와 함께 사용.")]
        public EnemySO[] enemyRoster;
    }
}
