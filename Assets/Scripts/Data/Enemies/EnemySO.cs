using UnityEngine;

namespace TOME.Data
{
    [CreateAssetMenu(menuName = "TOME/Enemy", fileName = "Enemy_")]
    public class EnemySO : ScriptableObject
    {
        public string id;
        public Sprite sprite;
        public RuntimeAnimatorController anim;

        [Header("Stats")]
        public int   hp = 30;
        public float atk = 1f;                  // 접촉(충돌) 데미지. 하트 1칸 = 1.0.
        public float moveSpeed = 1.5f;
        [Tooltip("적 비주얼 크기 배수. 보스는 2~3 등으로 크게.")]
        public float visualScale = 1f;
        [Tooltip("보스 여부(연출/표시용).")]
        public bool  isBoss;

        [Header("Ranged Attack (optional)")]
        public bool       hasRangedAttack;
        public GameObject bulletPrefab;         // EnemyBullet 컴포넌트가 붙은 프리팹
        public float      rangedDamage   = 0.5f;// 총알 데미지. 반 칸 = 0.5.
        public float      rangedRange    = 12f; // 이 거리 안에 들어오면 발사 (스폰 Y=4.5, 플레이어 ≈ -5 → 거리 10 안팎)
        public float      rangedCooldown = 3f;  // 발사 쿨다운(초). 너무 짧으면 회피 불가.
        public float      bulletSpeed    = 3.5f;// 회피 가능한 정도로 충분히 느리게

        [Header("Drop")]
        public ItemSO[] dropTable;
        public float[]  dropWeights;
        [Range(0,1)] public float dropChance = 0.5f;
    }
}
