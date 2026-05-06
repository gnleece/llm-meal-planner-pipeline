using MealPlannerPipeline.Core.Contracts;

namespace MealPlannerPipeline.Core.Pipeline;

public class PipelineRunResult
{
    public bool IsSuccess { get; private init; }
    public MealPlanOutput? MealPlan { get; private init; }
    public List<RecipeOutput>? Recipes { get; private init; }
    public GroceryOutput? GroceryList { get; private init; }
    public TimeSpan Duration { get; private init; }
    public string? FailedStage { get; private init; }
    public string? ErrorMessage { get; private init; }

    public static PipelineRunResult Success(
        MealPlanOutput plan,
        List<RecipeOutput> recipes,
        GroceryOutput grocery,
        TimeSpan duration) =>
        new()
        {
            IsSuccess = true,
            MealPlan = plan,
            Recipes = recipes,
            GroceryList = grocery,
            Duration = duration
        };

    public static PipelineRunResult Failed(string stage, string error) =>
        new() { IsSuccess = false, FailedStage = stage, ErrorMessage = error };
}
