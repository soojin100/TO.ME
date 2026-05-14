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
        public Sprite thumbnail;

        [Header("Combat")]
        public CharacterSO startCharacter;
        public float timeLimit = 60f;
        public EnemySpawnEntry[] spawns;

        [Header("Dialogue (CSV id)")]
        public string preDialogueId;
        public string postDialogueId;

        [Header("Rewards")]
        public RewardSO[] rewards;
    }
}
