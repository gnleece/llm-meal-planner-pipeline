using System.Text.Json;
using MealPlannerPipeline.Core.Contracts;
using MealPlannerPipeline.Core.Pipeline;
using MealPlannerPipeline.Infrastructure.Serialization;
using Microsoft.Extensions.Logging;

namespace MealPlannerPipeline.Stages.RecipeGeneratorStage;

public class RecipeGeneratorStage : IPipelineStage<PlannedMeal, RecipeOutput>
{
    private readonly ILlmClient _llmClient;
    private readonly ILogger<RecipeGeneratorStage> _logger;

    public string StageName => "RecipeGenerator";

    public RecipeGeneratorStage(ILlmClient llmClient, ILogger<RecipeGeneratorStage> logger)
    {
        _llmClient = llmClient;
        _logger = logger;
    }

    public async Task<StageResult<RecipeOutput>> ExecuteAsync(
        PlannedMeal input, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[{Stage}] Generating recipe for: {Meal}", StageName, input.MealName);

        var prompt = BuildPrompt(input);

        string raw;
        try
        {
            raw = await _llmClient.GenerateAsync(prompt, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{Stage}] LLM call failed for {Meal}", StageName, input.MealName);
            return StageResult<RecipeOutput>.Failure($"LLM call failed: {ex.Message}", prompt: prompt);
        }

        _logger.LogDebug("[{Stage}] Raw LLM output for {Meal}: {Raw}", StageName, input.MealName, raw);

        try
        {
            var sanitized = LlmJsonParser.SanitizeJson(raw);
            var result = JsonSerializer.Deserialize<RecipeOutput>(sanitized, JsonOptions.Default)
                         ?? throw new JsonException("Deserialized to null.");
            _logger.LogInformation("[{Stage}] Success — {Meal}: {IngCount} ingredients, {StepCount} steps",
                StageName, result.MealName, result.Ingredients.Count, result.Steps.Count);
            return StageResult<RecipeOutput>.Success(result, raw, prompt);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "[{Stage}] JSON parse failed for {Meal}. Raw output: {Raw}",
                StageName, input.MealName, raw);
            return StageResult<RecipeOutput>.Failure($"JSON parse failed: {ex.Message}", raw, prompt);
        }
    }

    private string BuildPrompt(PlannedMeal meal) => $$"""
        You are a recipe assistant. Return ONLY valid JSON with no explanation and no markdown.

        Generate a complete recipe for: {{meal.MealName}}
        Dietary requirements: {{string.Join(", ", meal.DietTags)}}
        Target calories: approximately {{meal.EstimatedCalories}}

        Return JSON in exactly this format:
        {
          "meal_name": "{{meal.MealName}}",
          "ingredients": [
            { "name": "ingredient name", "quantity": 1.0, "unit": "cup" }
          ],
          "steps": [
            "Step 1 description"
          ],
          "estimated_calories": {{meal.EstimatedCalories}}
        }

        Include at least 3 ingredients and at least 3 steps.
        """;
}
