using UnityEngine;

using TOME.Combat;
namespace TOME.Characters
{
    [CreateAssetMenu(menuName = "TOME/Character", fileName = "Char_")]
    public class CharacterSO : ScriptableObject
    {
        public string id;
        public string displayName;
        [TextArea(2, 4)] public string abilityDescription;
        public GameObject corePrefab;       // 비주얼 프리팹. CharacterCore(단일 sprite) 또는 PSB 본 리그(.psb) 직접 가능.
        public Sprite icon;
        public Color bodyTint = Color.white;   // 플레이스홀더 캐릭터 시각 구분용

        [Header("Visual (PSB 본 리그용 / 선택)")]
        [Tooltip("PSB 본 리그면 그 Animator Controller. 비어 있으면 단일 sprite/CharacterCore 방식.")]
        public RuntimeAnimatorController animatorController;
        [Tooltip("비주얼 인스턴스에 적용할 스케일. 0이면 미적용(1 유지).")]
        public float visualScale = 0f;
        [Tooltip("비주얼 인스턴스 root 이름 강제(애니 path가 root 이름 포함 시 매칭용). 예: Dog_halfside")]
        public string visualRootName = "";
        [Tooltip("정지 상태 Animator state 이름.")]
        public string idleStateName = "Idle";
        [Tooltip("이동 중 Animator state 이름. 비우면 walk 전환 안 함.")]
        public string walkStateName = "walk";

        [Header("Stats")]
        public int hp = 100;
        public int atk = 10;
        public float atkSpeed = 1f;         // 초당 공격
        public float atkRange = 4f;

        public AttackPatternSO attackPattern;
    }
}