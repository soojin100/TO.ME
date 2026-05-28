using UnityEngine;

namespace TOME.Data
{
    public enum RewardType { Coin, Item, Character }

    [CreateAssetMenu(menuName = "TOME/Reward", fileName = "Reward_")]
    public class RewardSO : ScriptableObject
    {
        public RewardType type;
        public int amount = 1;
        public ItemSO       item;        // type==Item
        public CharacterSO  character;   // type==Character
    }
}
