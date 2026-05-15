using MealPlannerPipeline.Core.Contracts;
using MealPlannerPipeline.Core.Pipeline;

namespace MealPlannerPipeline.Core.Evals;

public class GroceryEvaluator : IEvaluator<GroceryOutput>
{
    private readonly List<RecipeOutput> _recipes;

    public GroceryEvaluator(List<RecipeOutput> recipes)
    {
        _recipes = recipes;
    }

    public Task<EvalResult> EvaluateAsync(GroceryOutput grocery)
    {
        var issues = new List<string>();
        int passed = 0;
        const int checks = 3;

        // Check 1: no duplicate items (same name + unit)
        var keys = grocery.Items
            .Select(i => (i.Name.ToLowerInvariant(), i.Unit.ToLowerInvariant()))
            .ToList();
        if (keys.Count == keys.Distinct().Count())
            passed++;
        else
            issues.Add("Grocery list contains duplicate entries with the same name and unit.");

        // Build expected totals per (name, unit) from all recipe ingredients
        var expected = _recipes
            .SelectMany(r => r.Ingredients)
            .GroupBy(i => (Name: i.Name.ToLowerInvariant(), Unit: i.Unit.ToLowerInvariant()))
            .ToDictionary(g => g.Key, g => g.Sum(i => i.Quantity));

        // Group before building the lookup — the input may contain duplicates (that's what check 1 detects).
        // We still want checks 2 and 3 to run rather than throwing.
        var groceryLookup = grocery.Items
            .GroupBy(i => (i.Name.ToLowerInvariant(), i.Unit.ToLowerInvariant()))
            .ToDictionary(g => g.Key, g => g.Sum(i => i.TotalQuantity));

        // Check 2: all recipe ingredients represented in grocery list
        var missing = expected.Keys.Where(k => !groceryLookup.ContainsKey(k)).ToList();
        if (missing.Count == 0)
            passed++;
        else
            foreach (var m in missing)
                issues.Add($"Grocery list is missing ingredient: {m.Name} ({m.Unit}).");

        // Check 3: quantities aggregated correctly
        var quantityErrors = expected
            .Where(kv => groceryLookup.TryGetValue(kv.Key, out var actual) &&
                         Math.Abs(actual - kv.Value) > 0.001)
            .ToList();
        if (quantityErrors.Count == 0)
            passed++;
        else
            foreach (var e in quantityErrors)
                issues.Add($"Quantity mismatch for '{e.Key.Name}' ({e.Key.Unit}): expected {e.Value}, got {groceryLookup[e.Key]}.");

        return Task.FromResult(new EvalResult
        {
            Passed = issues.Count == 0,
            Score = (double)passed / checks,
            Issues = issues
        });
    }
}
