using UnityEngine;

namespace TOME.Data
{
    [System.Serializable]
    public class EnemySpawnEntry
    {
        public EnemySO enemy;
        public int     totalCount    = 3;     // 이 적이 등장하는 총 마릿수
        public int     simultaneous  = 1;     // 동시 등장 최대치
        public float   spawnInterval = 1.5f;  // 새 적 등장 간격(초)
        public float   startDelay;            // 스테이지 시작 후 첫 등장 지연
    }

    [CreateAssetMenu(menuName = "TOME/Stage", fileName = "Stage_")]
    public class StageSO : ScriptableObject
    {
        public string id;
        public string title;
        public ChapterSO chapter;                  // 소속 챕터(테마·적 로스터)
        [Range(1, 5)] public int difficulty = 1;   // 결과창 난이도 별 개수
        public Sprite thumbnail;
        [TextArea] public string introText;          // 스테이지 정보 팝업 소개문
        [TextArea] public string clearedIntroText;   // 클리어 후 표시할 소개문

        [Header("Audio")]
        [Tooltip("이 스테이지 전용 BGM. 비우면 Stage 씬 기본 BGM(BGM_Stage) 사용. 보스 스테이지는 BGM_Boss 지정.")]
        public AudioClip bgm;

        [Header("Combat")]
        public CharacterSO startCharacter;
        public float timeLimit = 60f;

        [Tooltip("자동 스폰용 적 로스터. spawns가 비어 있으면 difficulty와 이 로스터로 자동 생성. (ChapterSO 로스터로 대체 예정)")]
        public EnemySO[] enemyRoster;
        [Tooltip("비워두면 difficulty+enemyRoster로 자동 생성. 채우면 수동 오버라이드(스케일 미적용).")]
        public EnemySpawnEntry[] spawns;

        [Header("Dialogue (CSV id)")]
        public string preDialogueId;
        public string postDialogueId;

        [Header("Rewards")]
        public RewardSO[] rewards;
    }
}
