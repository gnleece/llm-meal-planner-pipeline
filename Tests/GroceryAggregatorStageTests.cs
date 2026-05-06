using MealPlannerPipeline.Core.Contracts;
using MealPlannerPipeline.Stages.GroceryAggregatorStage;
using Microsoft.Extensions.Logging.Abstractions;

namespace MealPlannerPipeline.Tests;

public class GroceryAggregatorStageTests
{
    private readonly GroceryAggregatorStage _stage =
        new(NullLogger<GroceryAggregatorStage>.Instance);

    [Fact]
    public async Task SumsQuantitiesForSameIngredient()
    {
        var recipes = new List<RecipeOutput>
        {
            MakeRecipe([new Ingredient("chickpeas", 1.0, "cup")]),
            MakeRecipe([new Ingredient("chickpeas", 2.0, "cup")])
        };

        var result = await _stage.ExecuteAsync(recipes);

        Assert.True(result.IsSuccess);
        var item = Assert.Single(result.Value!.Items);
        Assert.Equal("chickpeas", item.Name);
        Assert.Equal(3.0, item.TotalQuantity);
    }

    [Fact]
    public async Task TreatsIngredientNamesCaseInsensitively()
    {
        var recipes = new List<RecipeOutput>
        {
            MakeRecipe([new Ingredient("Spinach", 1.0, "cup")]),
            MakeRecipe([new Ingredient("spinach", 2.0, "cup")])
        };

        var result = await _stage.ExecuteAsync(recipes);

        Assert.True(result.IsSuccess);
        var item = Assert.Single(result.Value!.Items);
        Assert.Equal(3.0, item.TotalQuantity);
    }

    [Fact]
    public async Task TreatsUnitsCaseInsensitively()
    {
        var recipes = new List<RecipeOutput>
        {
            MakeRecipe([new Ingredient("olive oil", 1.0, "Tbsp")]),
            MakeRecipe([new Ingredient("olive oil", 2.0, "tbsp")])
        };

        var result = await _stage.ExecuteAsync(recipes);

        Assert.True(result.IsSuccess);
        var item = Assert.Single(result.Value!.Items);
        Assert.Equal(3.0, item.TotalQuantity);
    }

    [Fact]
    public async Task KeepsDifferentUnitsAsSeparateItems()
    {
        var recipes = new List<RecipeOutput>
        {
            MakeRecipe([new Ingredient("chickpeas", 1.0, "cup")]),
            MakeRecipe([new Ingredient("chickpeas", 200.0, "g")])
        };

        var result = await _stage.ExecuteAsync(recipes);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value!.Items.Count);
    }

    [Fact]
    public async Task HandlesSingleRecipe()
    {
        var recipes = new List<RecipeOutput>
        {
            MakeRecipe([new Ingredient("tomato", 2.0, "whole"), new Ingredient("garlic", 3.0, "clove")])
        };

        var result = await _stage.ExecuteAsync(recipes);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value!.Items.Count);
    }

    [Fact]
    public async Task HandlesEmptyRecipeList()
    {
        var result = await _stage.ExecuteAsync([]);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value!.Items);
    }

    [Fact]
    public async Task ReturnsItemsSortedByName()
    {
        var recipes = new List<RecipeOutput>
        {
            MakeRecipe([new Ingredient("zucchini", 1.0, "whole"), new Ingredient("apple", 2.0, "whole")])
        };

        var result = await _stage.ExecuteAsync(recipes);

        Assert.True(result.IsSuccess);
        var names = result.Value!.Items.Select(i => i.Name).ToList();
        Assert.Equal(names.OrderBy(n => n).ToList(), names);
    }

    private static RecipeOutput MakeRecipe(List<Ingredient> ingredients) =>
        new("Test Meal", ingredients, ["Step 1"], 400);
}
