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

    /// <summary>스테이지 안의 한 라운드. 라운드의 적을 전부 쓰러뜨리면 다음 라운드가 시작되고,
    /// 마지막 라운드까지 끝내야 스테이지 클리어다. 라운드는 스테이지 난이도를 단계적으로 올리는 수단이다.</summary>
    [System.Serializable]
    public class StageRound
    {
        [Tooltip("표시용 이름. 비우면 \"라운드 N\" 으로 보인다.")]
        public string label;
        [Tooltip("이 라운드의 난이도. spawns가 비었을 때 적 수·동시등장·간격·능력치를 여기서 산출한다.")]
        [Range(1, 5)] public int difficulty = 1;
        [Tooltip("이 라운드의 적 로스터. 비우면 스테이지 → 챕터 로스터를 쓴다.")]
        public EnemySO[] enemyRoster;
        [Tooltip("수동 스폰 목록. 채우면 difficulty 산출을 무시하고 이대로 나온다(보스 라운드 등).")]
        public EnemySpawnEntry[] spawns;
        [Tooltip("이전 라운드를 끝낸 뒤 이 라운드가 시작되기까지의 간격(초).")]
        [Min(0f)] public float startDelay = 1f;
        [Tooltip("이 라운드가 시작될 때 남은 제한시간에 더할 초. 라운드가 늘수록 시간도 늘려 준다.")]
        [Min(0f)] public float timeBonus;
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

        [Header("Rounds")]
        [Tooltip("라운드 구성. 앞 라운드의 적을 전부 잡아야 다음 라운드가 시작되고, 마지막 라운드를 끝내면 클리어다. " +
                 "비워두면 위의 spawns/difficulty로 한 라운드짜리 스테이지가 된다(기존 동작).")]
        public StageRound[] rounds;

        [Header("Dialogue (CSV id)")]
        public string preDialogueId;
        public string postDialogueId;

        [Header("Tutorial Constraints")]
        [Tooltip("false면 이 스테이지에서 아이템이 드랍되지 않는다. 튜토리얼 1 전투용(기획서 p15).")]
        public bool allowItemDrops = true;
        [Tooltip("false면 하단 인벤토리(조합창) 진입이 막힌다. 튜토리얼 1 전투용(기획서 p15).")]
        public bool allowInventory = true;
        [Tooltip("false면 이 스테이지 결과를 엔딩 점수에 집계하지 않는다. 집계 로직은 엔딩 시스템 구현 시 추가.")]
        public bool countsForEnding = true;

        [Header("Rewards")]
        public RewardSO[] rewards;
    }
}
