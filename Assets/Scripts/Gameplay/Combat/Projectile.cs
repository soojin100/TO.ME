using UnityEngine;
using TOME.Core;
using TOME.Gameplay.Enemy;

namespace TOME.Gameplay.Combat
{
    public class Projectile : MonoBehaviour
    {
        EnemyBase target;
        Vector3   lastTargetPos;
        float     speed;
        int       dmg;
        ObjectPool pool;
        const float HitDist = 0.2f;
        const float LifeTime = 3f;
        float life;

        public void Launch(EnemyBase tgt, float spd, int damage, ObjectPool owner)
        {
            target        = tgt;
            lastTargetPos = tgt ? tgt.transform.position : transform.position;
            speed         = spd;
            dmg           = damage;
            pool          = owner;
            life          = 0f;
        }

        void Update()
        {
            life += Time.deltaTime;
            if (life > LifeTime) { pool?.Release(gameObject); return; }

            // 타겟이 살아있으면 현재 위치 추적, 죽었으면 마지막 좌표로 진행
            if (target && target.IsAlive) lastTargetPos = target.transform.position;
            else                          target = null;

            transform.position = Vector3.MoveTowards(transform.position, lastTargetPos, speed * Time.deltaTime);
            if ((transform.position - lastTargetPos).sqrMagnitude < HitDist * HitDist)
            {
                if (target) target.TakeDamage(dmg);
                else
                {
                    var hit = EnemyRegistry.FindNearest(transform.position, HitDist * 2f);
                    if (hit) hit.TakeDamage(dmg);
                }
                pool?.Release(gameObject);
            }
        }
    }
}
