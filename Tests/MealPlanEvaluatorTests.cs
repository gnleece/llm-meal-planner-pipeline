using MealPlannerPipeline.Core.Contracts;
using MealPlannerPipeline.Core.Evals;

namespace MealPlannerPipeline.Tests;

public class MealPlanEvaluatorTests
{
    private readonly PipelineInput _constraints = new("vegetarian", 600, 2, 30);
    private readonly MealPlanEvaluator _evaluator;

    public MealPlanEvaluatorTests()
    {
        _evaluator = new MealPlanEvaluator(_constraints);
    }

    [Fact]
    public async Task ValidPlan_PassesAllChecks()
    {
        var plan = TwoValidMeals();

        var result = await _evaluator.EvaluateAsync(plan);

        Assert.True(result.Passed);
        Assert.Equal(1.0, result.Score);
        Assert.Empty(result.Issues);
    }

    [Fact]
    public async Task WrongMealCount_FailsWithIssue()
    {
        var plan = new MealPlanOutput([
            new PlannedMeal("Monday", "Salad", ["vegetarian"], 400)
        ]);

        var result = await _evaluator.EvaluateAsync(plan);

        Assert.False(result.Passed);
        Assert.Contains(result.Issues, i => i.Contains("Expected 2 meals, got 1"));
    }

    [Fact]
    public async Task MealExceedsCalorieLimit_FailsWithIssue()
    {
        var plan = new MealPlanOutput([
            new PlannedMeal("Monday", "Salad", ["vegetarian"], 400),
            new PlannedMeal("Tuesday", "Big Pasta", ["vegetarian"], 800)
        ]);

        var result = await _evaluator.EvaluateAsync(plan);

        Assert.False(result.Passed);
        Assert.Contains(result.Issues, i => i.Contains("Big Pasta") && i.Contains("800"));
    }

    [Fact]
    public async Task MissingDietTag_FailsWithIssue()
    {
        var plan = new MealPlanOutput([
            new PlannedMeal("Monday", "Steak", ["carnivore"], 500),
            new PlannedMeal("Tuesday", "Salad", ["vegetarian"], 400)
        ]);

        var result = await _evaluator.EvaluateAsync(plan);

        Assert.False(result.Passed);
        Assert.Contains(result.Issues, i => i.Contains("Steak") && i.Contains("vegetarian"));
    }

    [Fact]
    public async Task DietTagCaseInsensitive_Passes()
    {
        var plan = new MealPlanOutput([
            new PlannedMeal("Monday", "Salad", ["Vegetarian"], 400),
            new PlannedMeal("Tuesday", "Soup", ["VEGETARIAN"], 350)
        ]);

        var result = await _evaluator.EvaluateAsync(plan);

        Assert.True(result.Passed);
    }

    [Fact]
    public async Task DuplicateMealNames_FailsWithIssue()
    {
        var plan = new MealPlanOutput([
            new PlannedMeal("Monday", "Pasta", ["vegetarian"], 400),
            new PlannedMeal("Tuesday", "Pasta", ["vegetarian"], 450)
        ]);

        var result = await _evaluator.EvaluateAsync(plan);

        Assert.False(result.Passed);
        Assert.Contains(result.Issues, i => i.Contains("Duplicate"));
    }

    [Fact]
    public async Task MultipleIssues_ScoreReflectsFailedChecks()
    {
        // Wrong count + calories exceeded (only 1 meal, count is wrong AND calorie is over)
        var plan = new MealPlanOutput([
            new PlannedMeal("Monday", "Big Pasta", ["vegetarian"], 900)
        ]);

        var result = await _evaluator.EvaluateAsync(plan);

        Assert.False(result.Passed);
        Assert.True(result.Score < 1.0);
        Assert.True(result.Issues.Count >= 2);
    }

    private static MealPlanOutput TwoValidMeals() => new([
        new PlannedMeal("Monday", "Chickpea Curry", ["vegetarian"], 520),
        new PlannedMeal("Tuesday", "Lentil Soup", ["vegetarian"], 480)
    ]);
}
