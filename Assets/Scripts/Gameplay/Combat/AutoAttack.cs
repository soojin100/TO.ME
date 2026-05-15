using System;
using UnityEngine;
using TOME.Core;
using TOME.Data;
using TOME.Gameplay.Enemy;

namespace TOME.Gameplay.Combat
{
    /// 사거리 내 가장 가까운 적을 자동공격. 시간/공격속도 기반.
    public class AutoAttack : MonoBehaviour
    {
        AttackPatternSO pattern;
        Func<float> getAtk, getAtkSpd, getRange;
        float cooldown;
        ObjectPool projectilePool;

        public void Configure(AttackPatternSO p, Func<float> atk, Func<float> spd, Func<float> range)
        {
            getAtk   = atk;
            getAtkSpd= spd;
            getRange = range;

            // 패턴이 바뀔 때만 풀 재생성 (동일 패턴 재장착 시 누수 방지)
            if (p != pattern)
            {
                projectilePool = (p && p.projectilePrefab)
                    ? new ObjectPool(p.projectilePrefab, 8)
                    : null;
            }
            pattern = p;
        }

        void Update()
        {
            if (pattern == null) return;
            cooldown -= Time.deltaTime;
            if (cooldown > 0f) return;

            var target = EnemyRegistry.FindNearest(transform.position, getRange());
            if (!target) return;

            Fire(target);
            cooldown = 1f / Mathf.Max(0.1f, getAtkSpd());
        }

        void Fire(EnemyBase target)
        {
            if (pattern.type == AttackType.Projectile && projectilePool != null)
            {
                var go = projectilePool.Get(transform.position, Quaternion.identity);
                if (go.TryGetComponent<Projectile>(out var pj))
                    pj.Launch(target, pattern.projectileSpeed,
                              Mathf.RoundToInt(getAtk()), projectilePool);
            }
            else
            {
                target.TakeDamage(Mathf.RoundToInt(getAtk()));
            }
        }
    }
}
