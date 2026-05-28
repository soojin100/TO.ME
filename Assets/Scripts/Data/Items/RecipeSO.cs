using System.Linq;
using UnityEngine;

namespace TOME.Data
{
    [CreateAssetMenu(menuName = "TOME/Recipe", fileName = "Recipe_")]
    public class RecipeSO : ScriptableObject
    {
        public ItemSO[]    ingredients;     // 2~4개, 순서 무관
        public CharacterSO result;

        // 정렬된 id 시퀀스 → 매칭용 키
        public string Key()
        {
            if (ingredients == null || ingredients.Length == 0) return string.Empty;
            return string.Join(",", ingredients.Where(i => i).Select(i => i.id).OrderBy(s => s));
        }
    }
}
