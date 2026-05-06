namespace MealPlannerPipeline.Core.Pipeline;

public class StageResult<T>
{
    public bool IsSuccess { get; private init; }
    public T? Value { get; private init; }
    public string? ErrorMessage { get; private init; }
    public string? RawLlmOutput { get; private init; }

    // Populated by evaluators in Phase 2 — stages don't set this
    public EvalResult? EvalResult { get; set; }

    public static StageResult<T> Success(T value, string rawOutput) =>
        new() { IsSuccess = true, Value = value, RawLlmOutput = rawOutput };

    public static StageResult<T> Failure(string error, string? rawOutput = null) =>
        new() { IsSuccess = false, ErrorMessage = error, RawLlmOutput = rawOutput };
}
