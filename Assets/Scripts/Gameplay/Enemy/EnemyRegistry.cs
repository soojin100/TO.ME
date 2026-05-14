using System.Collections.Generic;
using UnityEngine;

namespace TOME.Gameplay.Enemy
{
    /// 매 프레임 FindObjectOfType 회피용. 자동공격이 가장 가까운 적 검색에 사용.
    public static class EnemyRegistry
    {
        static readonly List<EnemyBase> alive = new(32);

        public static void Register  (EnemyBase e) { if (e && !alive.Contains(e)) alive.Add(e); }
        public static void Unregister(EnemyBase e) { alive.Remove(e); }
        public static void Clear() => alive.Clear();

        public static EnemyBase FindNearest(Vector3 from, float maxRange)
        {
            EnemyBase best = null;
            float bestSq = maxRange * maxRange;
            for (int i = 0; i < alive.Count; i++)
            {
                var e = alive[i];
                if (!e) continue;
                float sq = (e.transform.position - from).sqrMagnitude;
                if (sq < bestSq) { bestSq = sq; best = e; }
            }
            return best;
        }
    }
}
