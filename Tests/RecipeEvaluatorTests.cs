using MealPlannerPipeline.Core.Contracts;
using MealPlannerPipeline.Core.Evals;

namespace MealPlannerPipeline.Tests;

public class RecipeEvaluatorTests
{
    private readonly PipelineInput _constraints = new("vegetarian", 600, 5, 30);
    private readonly RecipeEvaluator _evaluator;

    public RecipeEvaluatorTests()
    {
        _evaluator = new RecipeEvaluator(_constraints);
    }

    [Fact]
    public async Task ValidRecipe_PassesAllChecks()
    {
        var recipe = ValidRecipe();

        var result = await _evaluator.EvaluateAsync(recipe);

        Assert.True(result.Passed);
        Assert.Equal(1.0, result.Score);
        Assert.Empty(result.Issues);
    }

    [Fact]
    public async Task EmptyIngredients_FailsWithIssue()
    {
        var recipe = new RecipeOutput("Pasta", [], ["Boil water", "Cook pasta"], 400);

        var result = await _evaluator.EvaluateAsync(recipe);

        Assert.False(result.Passed);
        Assert.Contains(result.Issues, i => i.Contains("no ingredients"));
    }

    [Fact]
    public async Task EmptySteps_FailsWithIssue()
    {
        var recipe = new RecipeOutput("Pasta",
            [new Ingredient("pasta", 200, "g")],
            [],
            400);

        var result = await _evaluator.EvaluateAsync(recipe);

        Assert.False(result.Passed);
        Assert.Contains(result.Issues, i => i.Contains("no preparation steps"));
    }

    [Fact]
    public async Task CaloriesExceedLimit_FailsWithIssue()
    {
        var recipe = new RecipeOutput("Heavy Dish",
            [new Ingredient("cream", 500, "ml")],
            ["Mix everything"],
            900);

        var result = await _evaluator.EvaluateAsync(recipe);

        Assert.False(result.Passed);
        Assert.Contains(result.Issues, i => i.Contains("900") && i.Contains("600"));
    }

    [Fact]
    public async Task ForbiddenIngredient_Chicken_FailsWithIssue()
    {
        var recipe = new RecipeOutput("Surprise Dish",
            [new Ingredient("chicken", 200, "g"), new Ingredient("rice", 1, "cup")],
            ["Cook it"],
            500);

        var result = await _evaluator.EvaluateAsync(recipe);

        Assert.False(result.Passed);
        Assert.Contains(result.Issues, i => i.Contains("chicken") && i.Contains("forbidden"));
    }

    [Theory]
    [InlineData("beef")]
    [InlineData("pork")]
    [InlineData("fish")]
    [InlineData("Chicken")]
    [InlineData("BEEF")]
    public async Task ForbiddenIngredient_CaseInsensitive_FailsWithIssue(string ingredient)
    {
        var recipe = new RecipeOutput("Meat Dish",
            [new Ingredient(ingredient, 200, "g")],
            ["Cook it"],
            500);

        var result = await _evaluator.EvaluateAsync(recipe);

        Assert.False(result.Passed);
        Assert.Contains(result.Issues, i => i.Contains("forbidden"));
    }

    [Fact]
    public async Task NonVegetarianDiet_NoForbiddenIngredients()
    {
        var omnivoreEvaluator = new RecipeEvaluator(new PipelineInput("omnivore", 600, 5, 30));
        var recipe = new RecipeOutput("Chicken Dish",
            [new Ingredient("chicken", 200, "g")],
            ["Cook it"],
            400);

        var result = await omnivoreEvaluator.EvaluateAsync(recipe);

        Assert.True(result.Passed);
    }

    private static RecipeOutput ValidRecipe() => new(
        "Chickpea Curry",
        [new Ingredient("chickpeas", 2, "cups"), new Ingredient("spinach", 100, "g")],
        ["Heat oil", "Add spices", "Add chickpeas", "Simmer 20 min"],
        480);
}
