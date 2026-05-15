namespace MealPlannerPipeline.Core.Traces;

public record PipelineTrace
{
    public required string RunId { get; init; }
    public required DateTimeOffset StartedAt { get; init; }
    public TimeSpan TotalDuration { get; init; }
    public required List<StageTrace> Stages { get; init; }
}
