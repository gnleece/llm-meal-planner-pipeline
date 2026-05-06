namespace MealPlannerPipeline.Core.Pipeline;

public interface IPipelineStage<TInput, TOutput>
{
    string StageName { get; }
    Task<StageResult<TOutput>> ExecuteAsync(TInput input, CancellationToken cancellationToken = default);
}
