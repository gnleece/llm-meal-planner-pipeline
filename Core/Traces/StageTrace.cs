using MealPlannerPipeline.Core.Pipeline;

namespace MealPlannerPipeline.Core.Traces;

public record StageTrace
{
    public required string StageName { get; init; }
    public required string InputJson { get; init; }
    public string? Prompt { get; init; }
    public string? RawLlmOutput { get; init; }
    public string? OutputJson { get; init; }
    public EvalResult? Eval { get; init; }
    public int RetryCount { get; init; }
    public TimeSpan Latency { get; init; }
    public bool WasInjected { get; init; }
    public bool WasFailureInjected { get; init; }
    public bool IsSuccess { get; init; }
    public string? ErrorMessage { get; init; }
}
