using System;
using System.Collections.Generic;
using UnityEngine;

using TOME.Progression;
namespace TOME.Combat
{
    /// <summary>난이도(difficulty)와 적 로스터로 스폰 파라미터를 자동 산출. StageSO.spawns가 비어 있을 때 사용.</summary>
    public static class DifficultyScaler
    {
        // 적 능력치 배수 (diff1 = 1.0, diff5 ≈ 1.48)
        public static float StatMultiplier(int difficulty) => 1f + 0.12f * Mathf.Max(0, difficulty - 1);

        // 동시 등장 수 (diff1 → 1, diff5 → 3)
        public static int Simultaneous(int difficulty) => Mathf.Clamp(1 + (difficulty - 1) / 2, 1, 5);

        // 스폰 간격(초) — 난이도 높을수록 촘촘 (diff1 → 2.0, diff5 → 1.2)
        public static float SpawnInterval(int difficulty) => Mathf.Max(0.8f, 2.2f - 0.2f * difficulty);

        // 총 등장 수 (diff1 → 3, diff5 → 7)
        public static int TotalEnemies(int difficulty) => 2 + Mathf.Max(1, difficulty);

        /// 난이도+로스터로 EnemySpawnEntry[] 자동 생성. 로스터가 비면 빈 배열.
        public static EnemySpawnEntry[] BuildSpawns(int difficulty, EnemySO[] roster)
        {
            if (roster == null || roster.Length == 0) return Array.Empty<EnemySpawnEntry>();

            int total    = TotalEnemies(difficulty);
            int sim      = Simultaneous(difficulty);
            float gap    = SpawnInterval(difficulty);

            // 유효 로스터만 추림
            var types = new List<EnemySO>(roster.Length);
            foreach (var e in roster) if (e) types.Add(e);
            if (types.Count == 0) return Array.Empty<EnemySpawnEntry>();

            int perType = Mathf.CeilToInt(total / (float)types.Count);

            var list = new List<EnemySpawnEntry>(types.Count);
            for (int i = 0; i < types.Count; i++)
            {
                list.Add(new EnemySpawnEntry
                {
                    enemy         = types[i],
                    totalCount    = perType,
                    simultaneous  = sim,
                    spawnInterval = gap,
                    startDelay    = 0.5f + i * 0.5f
                });
            }
            return list.ToArray();
        }
    }
}
