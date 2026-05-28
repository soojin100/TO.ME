using UnityEngine;

namespace TOME.Data
{
    public enum ItemTier { Basic, Rare, Epic }

    [CreateAssetMenu(menuName = "TOME/Item", fileName = "Item_")]
    public class ItemSO : ScriptableObject
    {
        public string id;
        public string displayName;
        public Sprite icon;
        public ItemTier tier = ItemTier.Basic;
    }
}
