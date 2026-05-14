using MealPlannerPipeline.Core.Pipeline;

namespace MealPlannerPipeline.Core.Evals;

public interface IEvaluator<T>
{
    Task<EvalResult> EvaluateAsync(T value);
}
