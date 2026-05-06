using MealPlannerPipeline.Core.Contracts;
using MealPlannerPipeline.Core.Pipeline;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace MealPlannerPipeline.Tests;

public class MealPlannerOrchestratorTests
{
    private readonly Mock<IPipelineStage<PipelineInput, MealPlanOutput>> _mealPlannerMock = new();
    private readonly Mock<IPipelineStage<PlannedMeal, RecipeOutput>> _recipeGenMock = new();
    private readonly Mock<IPipelineStage<List<RecipeOutput>, GroceryOutput>> _groceryMock = new();

    private readonly MealPlannerOrchestrator _orchestrator;
    private readonly PipelineInput _input = new("vegetarian", 600, 2, 30);

    public MealPlannerOrchestratorTests()
    {
        _mealPlannerMock.Setup(s => s.StageName).Returns("MealPlanner");
        _recipeGenMock.Setup(s => s.StageName).Returns("RecipeGenerator");
        _groceryMock.Setup(s => s.StageName).Returns("GroceryAggregator");

        _orchestrator = new MealPlannerOrchestrator(
            _mealPlannerMock.Object,
            _recipeGenMock.Object,
            _groceryMock.Object,
            NullLogger<MealPlannerOrchestrator>.Instance);
    }

    [Fact]
    public async Task AllStagesSucceed_ReturnsSuccess()
    {
        SetupMealPlannerSuccess();
        SetupRecipeGeneratorSuccess();
        SetupGroceryAggregatorSuccess();

        var result = await _orchestrator.RunAsync(_input);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.MealPlan);
        Assert.NotNull(result.Recipes);
        Assert.NotNull(result.GroceryList);
    }

    [Fact]
    public async Task MealPlannerFails_HaltsImmediately()
    {
        _mealPlannerMock
            .Setup(s => s.ExecuteAsync(It.IsAny<PipelineInput>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(StageResult<MealPlanOutput>.Failure("LLM unavailable"));

        var result = await _orchestrator.RunAsync(_input);

        Assert.False(result.IsSuccess);
        Assert.Equal("MealPlanner", result.FailedStage);
        _recipeGenMock.Verify(
            s => s.ExecuteAsync(It.IsAny<PlannedMeal>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task AllRecipesFail_ReturnsPipelineFailure()
    {
        SetupMealPlannerSuccess();
        _recipeGenMock
            .Setup(s => s.ExecuteAsync(It.IsAny<PlannedMeal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(StageResult<RecipeOutput>.Failure("parse error"));

        var result = await _orchestrator.RunAsync(_input);

        Assert.False(result.IsSuccess);
        Assert.Equal("RecipeGenerator", result.FailedStage);
    }

    [Fact]
    public async Task OneRecipeFails_ContinuesWithPartialList()
    {
        SetupMealPlannerSuccess();

        // PlannedMeal has List<string> DietTags which uses reference equality, so we match by name
        _recipeGenMock
            .Setup(s => s.ExecuteAsync(It.Is<PlannedMeal>(m => m.MealName == "Chickpea Curry"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(StageResult<RecipeOutput>.Success(MakeRecipe("Chickpea Curry"), string.Empty));
        _recipeGenMock
            .Setup(s => s.ExecuteAsync(It.Is<PlannedMeal>(m => m.MealName == "Lentil Soup"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(StageResult<RecipeOutput>.Failure("parse error"));

        SetupGroceryAggregatorSuccess();

        var result = await _orchestrator.RunAsync(_input);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Recipes!);
    }

    [Fact]
    public async Task GroceryAggregatorFails_ReturnsFailure()
    {
        SetupMealPlannerSuccess();
        SetupRecipeGeneratorSuccess();
        _groceryMock
            .Setup(s => s.ExecuteAsync(It.IsAny<List<RecipeOutput>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(StageResult<GroceryOutput>.Failure("aggregation error"));

        var result = await _orchestrator.RunAsync(_input);

        Assert.False(result.IsSuccess);
        Assert.Equal("GroceryAggregator", result.FailedStage);
    }

    private List<PlannedMeal> TwoMeals() =>
    [
        new PlannedMeal("Monday", "Chickpea Curry", ["vegetarian"], 520),
        new PlannedMeal("Tuesday", "Lentil Soup", ["vegetarian"], 480)
    ];

    private void SetupMealPlannerSuccess()
    {
        var meals = TwoMeals();
        _mealPlannerMock
            .Setup(s => s.ExecuteAsync(It.IsAny<PipelineInput>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(StageResult<MealPlanOutput>.Success(new MealPlanOutput(meals), string.Empty));
    }

    private void SetupRecipeGeneratorSuccess()
    {
        _recipeGenMock
            .Setup(s => s.ExecuteAsync(It.IsAny<PlannedMeal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PlannedMeal meal, CancellationToken _) =>
                StageResult<RecipeOutput>.Success(MakeRecipe(meal.MealName), string.Empty));
    }

    private void SetupGroceryAggregatorSuccess()
    {
        _groceryMock
            .Setup(s => s.ExecuteAsync(It.IsAny<List<RecipeOutput>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(StageResult<GroceryOutput>.Success(
                new GroceryOutput([new GroceryItem("chickpeas", 2.0, "cup")]),
                string.Empty));
    }

    private static RecipeOutput MakeRecipe(string name) =>
        new(name, [new Ingredient("chickpeas", 1.0, "cup")], ["Step 1"], 500);
}
