using UnityEngine;
using TOME.Data;

namespace TOME.Gameplay.Player
{
    /// <summary>조합 결과 캐릭터 프리팹 루트. 외형·애니메이션만 책임.</summary>
    public class CharacterCore : MonoBehaviour
    {
        public CharacterSO Def { get; private set; }

        [SerializeField] SpriteRenderer body;
        [SerializeField] Animator       animator;
        [SerializeField] string         idleStateName = "Idle";

        public void Bind(CharacterSO def)
        {
            Def = def;
            if (body && def.icon) body.sprite = def.icon;
            if (animator && !string.IsNullOrEmpty(idleStateName)) animator.Play(idleStateName, 0, 0f);
        }
    }
}
