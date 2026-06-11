using System.Linq;
using UnityEngine;

namespace TOME.Data
{
    [CreateAssetMenu(menuName = "TOME/Recipe", fileName = "Recipe_")]
    public class RecipeSO : ScriptableObject
    {
        public ItemSO[]    ingredients;     // 2~4개, 순서 무관
        public CharacterSO result;          // 아트(캐릭터 프리팹) 생기면 연결. 없으면 비움.

        [Header("결과물 데이터 (조합표)")]
        public string resultId;             // 조합표 번호 등 식별자
        public string resultName;           // 결과물 이름 (예: 적토견)
        [TextArea] public string ability;   // 고유 능력 설명
        [TextArea] public string notes;     // 기타 / 수정사항

        // 정렬된 id 시퀀스 → 매칭용 키
        public string Key()
        {
            if (ingredients == null || ingredients.Length == 0) return string.Empty;
            return string.Join(",", ingredients.Where(i => i).Select(i => i.id).OrderBy(s => s));
        }
    }
}
