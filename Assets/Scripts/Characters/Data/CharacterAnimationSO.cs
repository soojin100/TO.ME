using UnityEngine;

namespace TOME.Characters
{
    [CreateAssetMenu(menuName = "TOME/Character Animation", fileName = "CharAnim_")]
    public class CharacterAnimationSO : ScriptableObject
    {
        public string animationId;          // 코드에서 호출할 ID
        public string animatorParamName;    // Animator 파라미터 이름
        public AnimParamType paramType;     // 파라미터 타입
        public bool blockWander = true;     // 재생 중 배회 중단 여부
        public bool waitForEnd = true;     // 끝날 때까지 대기 여부

        public enum AnimParamType { Trigger, Bool }
    }
}