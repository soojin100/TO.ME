using System.Collections.Generic;
using System.Linq;
using TOME.Data;

namespace TOME.Gameplay.Merge
{
    /// 레시피 목록을 정렬키 → SO 로 캐시. 매칭 O(1).
    /// 인스턴스 단위라 서로 다른 조합 규칙 집합이 동시에 공존해도 충돌하지 않는다.
    public class RecipeMatcher
    {
        readonly Dictionary<string, RecipeSO> cache = new(32);

        public void Init(IEnumerable<RecipeSO> recipes)
        {
            cache.Clear();
            if (recipes == null) return;
            foreach (var r in recipes)
            {
                if (!r || r.ingredients == null) continue;
                var key = r.Key();
                if (!string.IsNullOrEmpty(key)) cache[key] = r;
            }
        }

        public RecipeSO Match(IList<ItemSO> ingredients)
        {
            if (ingredients == null || ingredients.Count == 0) return null;
            var key = string.Join(",", ingredients.Where(i => i).Select(i => i.id).OrderBy(s => s));
            return cache.TryGetValue(key, out var r) ? r : null;
        }
    }
}
