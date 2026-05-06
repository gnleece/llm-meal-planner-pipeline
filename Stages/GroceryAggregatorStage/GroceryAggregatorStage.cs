using MealPlannerPipeline.Core.Contracts;
using MealPlannerPipeline.Core.Pipeline;
using Microsoft.Extensions.Logging;

namespace MealPlannerPipeline.Stages.GroceryAggregatorStage;

// Intentionally no LLM call — aggregation is a deterministic operation.
// Using an LLM here would introduce unnecessary failure surface.
public class GroceryAggregatorStage : IPipelineStage<List<RecipeOutput>, GroceryOutput>
{
    private readonly ILogger<GroceryAggregatorStage> _logger;

    public string StageName => "GroceryAggregator";

    public GroceryAggregatorStage(ILogger<GroceryAggregatorStage> logger)
    {
        _logger = logger;
    }

    public Task<StageResult<GroceryOutput>> ExecuteAsync(
        List<RecipeOutput> input, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[{Stage}] Aggregating ingredients from {Count} recipes", StageName, input.Count);

        try
        {
            var aggregated = input
                .SelectMany(r => r.Ingredients)
                .GroupBy(i => (Name: i.Name.ToLowerInvariant(), Unit: i.Unit.ToLowerInvariant()))
                .Select(g => new GroceryItem(
                    Name: g.Key.Name,
                    TotalQuantity: g.Sum(i => i.Quantity),
                    Unit: g.Key.Unit))
                .OrderBy(i => i.Name)
                .ToList();

            // TODO Phase 3: unit normalization (e.g. "2 cups" + "200g" of same ingredient)
            _logger.LogInformation("[{Stage}] Aggregated {Count} unique grocery items", StageName, aggregated.Count);
            return Task.FromResult(StageResult<GroceryOutput>.Success(new GroceryOutput(aggregated), string.Empty));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{Stage}] Aggregation failed", StageName);
            return Task.FromResult(StageResult<GroceryOutput>.Failure($"Aggregation failed: {ex.Message}"));
        }
    }
}
