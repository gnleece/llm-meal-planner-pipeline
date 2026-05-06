namespace MealPlannerPipeline.Core.Pipeline;

public class EvalResult
{
    public bool Passed { get; init; }
    public double Score { get; init; }
    public List<string> Issues { get; init; } = new();
}
