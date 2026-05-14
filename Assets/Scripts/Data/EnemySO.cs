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
        public int   atk = 5;
        public float moveSpeed = 1.5f;

        [Header("Drop")]
        public ItemSO[] dropTable;
        public float[]  dropWeights;
        [Range(0,1)] public float dropChance = 0.5f;
    }
}
