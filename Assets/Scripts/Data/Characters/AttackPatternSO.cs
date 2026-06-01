using UnityEngine;

namespace TOME.Data
{
    public enum AttackType { Projectile, Melee, Aoe }

    [CreateAssetMenu(menuName = "TOME/AttackPattern", fileName = "Atk_")]
    public class AttackPatternSO : ScriptableObject
    {
        public AttackType type = AttackType.Projectile;
        public GameObject projectilePrefab;     // 풀링 대상
        public float projectileSpeed = 12f;
        public float aoeRadius;
        public int pierce;                      // 관통 횟수
    }
}
