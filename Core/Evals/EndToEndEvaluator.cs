using MealPlannerPipeline.Core.Contracts;
using MealPlannerPipeline.Core.Pipeline;

namespace MealPlannerPipeline.Core.Evals;

// Not generic — aggregates results across all three stage outputs.
public class EndToEndEvaluator
{
    private static readonly HashSet<string> VegetarianForbidden =
        new(StringComparer.OrdinalIgnoreCase) { "chicken", "beef", "pork", "fish" };

    private readonly PipelineInput _constraints;

    public EndToEndEvaluator(PipelineInput constraints)
    {
        _constraints = constraints;
    }

    public EvalResult Evaluate(MealPlanOutput plan, List<RecipeOutput> recipes, GroceryOutput grocery)
    {
        var issues = new List<string>();
        int passed = 0;
        const int checks = 3;

        // Check 1: overall dietary compliance across all recipe ingredients
        var forbidden = GetForbiddenIngredients();
        var dietViolations = recipes
            .SelectMany(r => r.Ingredients.Select(i => (Recipe: r.MealName, Ingredient: i.Name)))
            .Where(x => forbidden.Contains(x.Ingredient))
            .ToList();
        if (dietViolations.Count == 0)
            passed++;
        else
            foreach (var v in dietViolations)
                issues.Add($"Diet violation in '{v.Recipe}': '{v.Ingredient}' is forbidden for {_constraints.Diet}.");

        // Check 2: overall calorie compliance across all planned meals
        var calorieViolations = plan.Days
            .Where(m => m.EstimatedCalories > _constraints.MaxCalories)
            .ToList();
        if (calorieViolations.Count == 0)
            passed++;
        else
            foreach (var m in calorieViolations)
                issues.Add($"Calorie violation: '{m.MealName}' = {m.EstimatedCalories} > {_constraints.MaxCalories}.");

        // Check 3: grocery list covers every unique (ingredient, unit) from all recipes
        var expectedKeys = recipes
            .SelectMany(r => r.Ingredients)
            .Select(i => (i.Name.ToLowerInvariant(), i.Unit.ToLowerInvariant()))
            .ToHashSet();
        var groceryKeys = grocery.Items
            .Select(i => (i.Name.ToLowerInvariant(), i.Unit.ToLowerInvariant()))
            .ToHashSet();
        var missingFromGrocery = expectedKeys.Except(groceryKeys).ToList();
        if (missingFromGrocery.Count == 0)
            passed++;
        else
            foreach (var m in missingFromGrocery)
                issues.Add($"Grocery missing: '{m.Item1}' ({m.Item2}).");

        return new EvalResult
        {
            Passed = issues.Count == 0,
            Score = (double)passed / checks,
            Issues = issues
        };
    }

    private HashSet<string> GetForbiddenIngredients() =>
        _constraints.Diet.Equals("vegetarian", StringComparison.OrdinalIgnoreCase)
            ? VegetarianForbidden
            : new HashSet<string>(StringComparer.OrdinalIgnoreCase);
}
