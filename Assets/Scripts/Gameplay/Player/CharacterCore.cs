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
            if (body)
            {
                if (def.icon) body.sprite = def.icon;
                body.color = def.bodyTint;
            }
            if (animator && !string.IsNullOrEmpty(idleStateName)) animator.Play(idleStateName, 0, 0f);
        }
    }
}
