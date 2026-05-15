using MealPlannerPipeline.Core.Contracts;

namespace MealPlannerPipeline.Core.Pipeline;

public enum FailureMode
{
    MalformedJson,
    DietaryViolation,
    MissingIngredients,
    ExceedCalories,
}

public record PipelineOptions
{
    public static readonly PipelineOptions Default = new();

    public MealPlanOutput? KnownGoodMealPlan { get; init; }
    public List<RecipeOutput>? KnownGoodRecipes { get; init; }
    public FailureMode? InjectMealPlannerFailure { get; init; }
    public FailureMode? InjectRecipeFailure { get; init; }
}
