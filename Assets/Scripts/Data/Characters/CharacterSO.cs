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
        public Color  bodyTint = Color.white;   // 플레이스홀더 캐릭터 시각 구분용

        [Header("Stats")]
        public int   hp = 100;
        public int   atk = 10;
        public float atkSpeed = 1f;         // 초당 공격
        public float atkRange = 4f;

        public AttackPatternSO attackPattern;
    }
}
