using System.Text.Json;
using MealPlannerPipeline.Core.Contracts;
using MealPlannerPipeline.Core.Pipeline;
using MealPlannerPipeline.Infrastructure.Serialization;
using Microsoft.Extensions.Logging;

namespace MealPlannerPipeline.Stages.MealPlannerStage;

public class MealPlannerStage : IPipelineStage<PipelineInput, MealPlanOutput>
{
    private readonly ILlmClient _llmClient;
    private readonly ILogger<MealPlannerStage> _logger;

    public string StageName => "MealPlanner";

    public MealPlannerStage(ILlmClient llmClient, ILogger<MealPlannerStage> logger)
    {
        _llmClient = llmClient;
        _logger = logger;
    }

    public async Task<StageResult<MealPlanOutput>> ExecuteAsync(
        PipelineInput input, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[{Stage}] Starting — {Days} days, diet={Diet}, maxCal={MaxCal}",
            StageName, input.Days, input.Diet, input.MaxCalories);

        var prompt = BuildPrompt(input);

        string raw;
        try
        {
            raw = await _llmClient.GenerateAsync(prompt, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{Stage}] LLM call failed", StageName);
            return StageResult<MealPlanOutput>.Failure($"LLM call failed: {ex.Message}");
        }

        _logger.LogDebug("[{Stage}] Raw LLM output: {Raw}", StageName, raw);

        try
        {
            var sanitized = LlmJsonParser.SanitizeJson(raw);
            var result = JsonSerializer.Deserialize<MealPlanOutput>(sanitized, JsonOptions.Default)
                         ?? throw new JsonException("Deserialized to null.");
            _logger.LogInformation("[{Stage}] Success — {Count} meals planned", StageName, result.Days.Count);
            return StageResult<MealPlanOutput>.Success(result, raw);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "[{Stage}] JSON parse failed. Raw output: {Raw}", StageName, raw);
            return StageResult<MealPlanOutput>.Failure($"JSON parse failed: {ex.Message}", raw);
        }
    }

    private string BuildPrompt(PipelineInput input) => $$"""
        You are a meal planning assistant. Return ONLY valid JSON with no explanation and no markdown.

        Generate a meal plan with exactly {{input.Days}} meals satisfying these constraints:
        - Diet: {{input.Diet}}
        - Maximum calories per meal: {{input.MaxCalories}}
        - Maximum prep time: {{input.MaxPrepTimeMinutes}} minutes

        Return JSON in exactly this format:
        {
          "days": [
            {
              "day": "Monday",
              "meal_name": "Example Meal",
              "diet_tags": ["{{input.Diet}}"],
              "estimated_calories": 500
            }
          ]
        }

        Generate exactly {{input.Days}} meals. Use day names Monday through Sunday as needed.
        """;
}
