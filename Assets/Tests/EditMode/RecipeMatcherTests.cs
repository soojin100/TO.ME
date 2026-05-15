using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;
using TOME.Data;
using TOME.Gameplay.Merge;

namespace TOME.Tests.EditMode
{
    public class RecipeMatcherTests
    {
        readonly List<ScriptableObject> _created = new();

        ItemSO MakeItem(string id)
        {
            var it = ScriptableObject.CreateInstance<ItemSO>();
            it.id = id;
            _created.Add(it);
            return it;
        }

        RecipeSO MakeRecipe(params ItemSO[] ingredients)
        {
            var r = ScriptableObject.CreateInstance<RecipeSO>();
            r.ingredients = ingredients;
            _created.Add(r);
            return r;
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var o in _created) Object.DestroyImmediate(o);
            _created.Clear();
        }

        [Test]
        public void Match_IngredientOrderIndependent()
        {
            var potion = MakeItem("potion");
            var star = MakeItem("star");
            var recipe = MakeRecipe(potion, star);
            var matcher = new RecipeMatcher();
            matcher.Init(new[] { recipe });

            var matchAB = matcher.Match(new List<ItemSO> { potion, star });
            var matchBA = matcher.Match(new List<ItemSO> { star, potion });

            Assert.AreSame(recipe, matchAB);
            Assert.AreSame(recipe, matchBA);
        }

        [Test]
        public void Match_UnknownCombo_ReturnsNull()
        {
            var potion = MakeItem("potion");
            var star = MakeItem("star");
            var sword = MakeItem("sword");
            var matcher = new RecipeMatcher();
            matcher.Init(new[] { MakeRecipe(potion, star) });

            var result = matcher.Match(new List<ItemSO> { potion, sword });

            Assert.IsNull(result);
        }
    }
}
