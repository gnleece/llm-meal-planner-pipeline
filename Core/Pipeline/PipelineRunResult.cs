using MealPlannerPipeline.Core.Contracts;
using MealPlannerPipeline.Core.Traces;

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

    // Phase 2: per-stage and end-to-end eval results
    public EvalResult? MealPlanEval { get; private init; }
    public IReadOnlyList<EvalResult>? RecipeEvals { get; private init; }
    public EvalResult? GroceryEval { get; private init; }
    public EvalResult? EndToEndEval { get; private init; }

    // Phase 3: full pipeline trace
    public PipelineTrace? Trace { get; private init; }

    public static PipelineRunResult Success(
        MealPlanOutput plan,
        List<RecipeOutput> recipes,
        GroceryOutput grocery,
        EvalResult mealPlanEval,
        IReadOnlyList<EvalResult> recipeEvals,
        EvalResult groceryEval,
        EvalResult endToEndEval,
        TimeSpan duration,
        PipelineTrace trace) =>
        new()
        {
            IsSuccess = true,
            MealPlan = plan,
            Recipes = recipes,
            GroceryList = grocery,
            MealPlanEval = mealPlanEval,
            RecipeEvals = recipeEvals,
            GroceryEval = groceryEval,
            EndToEndEval = endToEndEval,
            Duration = duration,
            Trace = trace,
        };

    public static PipelineRunResult Failed(string stage, string error, PipelineTrace? trace = null) =>
        new() { IsSuccess = false, FailedStage = stage, ErrorMessage = error, Trace = trace };
}
