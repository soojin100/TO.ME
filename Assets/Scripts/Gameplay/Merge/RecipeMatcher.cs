using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TOME.Data;

namespace TOME.Gameplay.Merge
{
    /// 부팅 시 모든 RecipeSO를 정렬키 → SO 로 캐시. 매칭 O(1).
    public static class RecipeMatcher
    {
        static Dictionary<string, RecipeSO> cache;

        public static void Init(IEnumerable<RecipeSO> recipes)
        {
            cache = new Dictionary<string, RecipeSO>(32);
            foreach (var r in recipes)
            {
                if (!r || r.ingredients == null) continue;
                var key = r.Key();
                if (!string.IsNullOrEmpty(key)) cache[key] = r;
            }
        }

        public static RecipeSO Match(IList<ItemSO> ingredients)
        {
            if (cache == null || ingredients == null || ingredients.Count == 0) return null;
            var key = string.Join(",", ingredients.Where(i => i).Select(i => i.id).OrderBy(s => s));
            return cache.TryGetValue(key, out var r) ? r : null;
        }
    }
}
