using UnityEngine;

namespace TOME.Data
{
    [CreateAssetMenu(menuName = "TOME/Character", fileName = "Char_")]
    public class CharacterSO : ScriptableObject
    {
        public string id;
        public string displayName;
        public GameObject corePrefab;       // CharacterCore 가진 프리팹
        public Sprite icon;

        [Header("Stats")]
        public int   hp = 100;
        public int   atk = 10;
        public float atkSpeed = 1f;         // 초당 공격
        public float atkRange = 4f;

        public AttackPatternSO attackPattern;
    }
}
