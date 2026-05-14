using MealPlannerPipeline.Core.Contracts;
using MealPlannerPipeline.Core.Evals;

namespace MealPlannerPipeline.Tests;

public class GroceryEvaluatorTests
{
    [Fact]
    public async Task ValidGrocery_PassesAllChecks()
    {
        var recipes = TwoRecipes();
        var grocery = new GroceryOutput([
            new GroceryItem("chickpeas", 3.0, "cups"),
            new GroceryItem("spinach", 200.0, "g")
        ]);

        var result = await new GroceryEvaluator(recipes).EvaluateAsync(grocery);

        Assert.True(result.Passed);
        Assert.Equal(1.0, result.Score);
        Assert.Empty(result.Issues);
    }

    [Fact]
    public async Task DuplicateGroceryItems_FailsWithIssue()
    {
        var recipes = TwoRecipes();
        var grocery = new GroceryOutput([
            new GroceryItem("chickpeas", 2.0, "cups"),
            new GroceryItem("chickpeas", 1.0, "cups"),  // duplicate name+unit
            new GroceryItem("spinach", 200.0, "g")
        ]);

        var result = await new GroceryEvaluator(recipes).EvaluateAsync(grocery);

        Assert.False(result.Passed);
        Assert.Contains(result.Issues, i => i.Contains("duplicate"));
    }

    [Fact]
    public async Task MissingIngredient_FailsWithIssue()
    {
        var recipes = TwoRecipes();
        var grocery = new GroceryOutput([
            new GroceryItem("chickpeas", 3.0, "cups")
            // spinach is missing
        ]);

        var result = await new GroceryEvaluator(recipes).EvaluateAsync(grocery);

        Assert.False(result.Passed);
        Assert.Contains(result.Issues, i => i.Contains("spinach"));
    }

    [Fact]
    public async Task IncorrectQuantity_FailsWithIssue()
    {
        var recipes = TwoRecipes();
        var grocery = new GroceryOutput([
            new GroceryItem("chickpeas", 1.0, "cups"),  // should be 3.0
            new GroceryItem("spinach", 200.0, "g")
        ]);

        var result = await new GroceryEvaluator(recipes).EvaluateAsync(grocery);

        Assert.False(result.Passed);
        Assert.Contains(result.Issues, i => i.Contains("chickpeas") && i.Contains("mismatch"));
    }

    [Fact]
    public async Task EmptyRecipeList_EmptyGrocery_Passes()
    {
        var result = await new GroceryEvaluator([]).EvaluateAsync(new GroceryOutput([]));

        Assert.True(result.Passed);
    }

    [Fact]
    public async Task DifferentUnitsKeptSeparate_BothMustBePresent()
    {
        var recipes = new List<RecipeOutput>
        {
            new("Dish A", [new Ingredient("chickpeas", 1.0, "cup")], ["Step 1"], 400),
            new("Dish B", [new Ingredient("chickpeas", 200.0, "g")], ["Step 1"], 400)
        };
        var grocery = new GroceryOutput([
            new GroceryItem("chickpeas", 1.0, "cup"),
            new GroceryItem("chickpeas", 200.0, "g")
        ]);

        var result = await new GroceryEvaluator(recipes).EvaluateAsync(grocery);

        Assert.True(result.Passed);
    }

    // Two recipes totalling: chickpeas=3.0 cups, spinach=200g
    private static List<RecipeOutput> TwoRecipes() =>
    [
        new("Chickpea Curry",
            [new Ingredient("chickpeas", 2.0, "cups"), new Ingredient("spinach", 100.0, "g")],
            ["Step 1"], 480),
        new("Chickpea Salad",
            [new Ingredient("chickpeas", 1.0, "cups"), new Ingredient("spinach", 100.0, "g")],
            ["Step 1"], 350)
    ];
}
