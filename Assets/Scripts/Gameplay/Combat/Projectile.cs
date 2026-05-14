using UnityEngine;
using TOME.Core;
using TOME.Gameplay.Enemy;

namespace TOME.Gameplay.Combat
{
    public class Projectile : MonoBehaviour
    {
        Vector3 targetPos;
        float   speed;
        int     dmg;
        ObjectPool pool;
        const float HitDist = 0.2f;
        const float LifeTime = 3f;
        float life;

        public void Launch(Vector3 target, float spd, int damage, ObjectPool owner)
        {
            targetPos = target;
            speed     = spd;
            dmg       = damage;
            pool      = owner;
            life      = 0f;
        }

        void Update()
        {
            life += Time.deltaTime;
            if (life > LifeTime) { pool?.Release(gameObject); return; }
            transform.position = Vector3.MoveTowards(transform.position, targetPos, speed * Time.deltaTime);
            if ((transform.position - targetPos).sqrMagnitude < HitDist * HitDist)
            {
                var hit = EnemyRegistry.FindNearest(transform.position, HitDist * 2f);
                if (hit) hit.TakeDamage(dmg);
                pool?.Release(gameObject);
            }
        }
    }
}
