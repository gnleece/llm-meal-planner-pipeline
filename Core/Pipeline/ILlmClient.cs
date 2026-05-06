namespace MealPlannerPipeline.Core.Pipeline;

public interface ILlmClient
{
    Task<string> GenerateAsync(string prompt, CancellationToken cancellationToken = default);
}
