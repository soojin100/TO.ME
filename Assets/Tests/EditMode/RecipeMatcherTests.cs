using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;
using TOME.Data;
using TOME.Gameplay.Merge;

namespace TOME.Tests.EditMode
{
    public class RecipeMatcherTests
    {
        static ItemSO MakeItem(string id)
        {
            var it = ScriptableObject.CreateInstance<ItemSO>();
            it.id = id;
            return it;
        }

        static RecipeSO MakeRecipe(params ItemSO[] ingredients)
        {
            var r = ScriptableObject.CreateInstance<RecipeSO>();
            r.ingredients = ingredients;
            return r;
        }

        [Test]
        public void Match_IngredientOrderIndependent()
        {
            var potion = MakeItem("potion");
            var star = MakeItem("star");
            var recipe = MakeRecipe(potion, star);
            RecipeMatcher.Init(new[] { recipe });

            var matchAB = RecipeMatcher.Match(new List<ItemSO> { potion, star });
            var matchBA = RecipeMatcher.Match(new List<ItemSO> { star, potion });

            Assert.AreSame(recipe, matchAB);
            Assert.AreSame(recipe, matchBA);
        }

        [Test]
        public void Match_UnknownCombo_ReturnsNull()
        {
            var potion = MakeItem("potion");
            var star = MakeItem("star");
            var sword = MakeItem("sword");
            RecipeMatcher.Init(new[] { MakeRecipe(potion, star) });

            var result = RecipeMatcher.Match(new List<ItemSO> { potion, sword });

            Assert.IsNull(result);
        }
    }
}
