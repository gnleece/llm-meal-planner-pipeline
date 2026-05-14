using MealPlannerPipeline.Core.Contracts;
using MealPlannerPipeline.Core.Pipeline;

namespace MealPlannerPipeline.Core.Evals;

public class RecipeEvaluator : IEvaluator<RecipeOutput>
{
    private static readonly HashSet<string> VegetarianForbidden =
        new(StringComparer.OrdinalIgnoreCase) { "chicken", "beef", "pork", "fish" };

    private readonly PipelineInput _constraints;

    public RecipeEvaluator(PipelineInput constraints)
    {
        _constraints = constraints;
    }

    public Task<EvalResult> EvaluateAsync(RecipeOutput recipe)
    {
        var issues = new List<string>();
        int passed = 0;
        const int checks = 4;

        // Check 1: ingredients list non-empty
        if (recipe.Ingredients.Count > 0)
            passed++;
        else
            issues.Add($"'{recipe.MealName}' has no ingredients.");

        // Check 2: steps list non-empty
        if (recipe.Steps.Count > 0)
            passed++;
        else
            issues.Add($"'{recipe.MealName}' has no preparation steps.");

        // Check 3: recipe calories within limit
        if (recipe.EstimatedCalories <= _constraints.MaxCalories)
            passed++;
        else
            issues.Add($"'{recipe.MealName}' exceeds calorie limit: {recipe.EstimatedCalories} > {_constraints.MaxCalories}.");

        // Check 4: no forbidden ingredients for the specified diet
        var forbidden = GetForbiddenIngredients();
        var violations = recipe.Ingredients
            .Where(i => forbidden.Contains(i.Name))
            .Select(i => i.Name)
            .ToList();
        if (violations.Count == 0)
            passed++;
        else
            issues.Add($"'{recipe.MealName}' contains forbidden ingredient(s) for {_constraints.Diet}: {string.Join(", ", violations)}.");

        return Task.FromResult(new EvalResult
        {
            Passed = issues.Count == 0,
            Score = (double)passed / checks,
            Issues = issues
        });
    }

    private HashSet<string> GetForbiddenIngredients() =>
        _constraints.Diet.Equals("vegetarian", StringComparison.OrdinalIgnoreCase)
            ? VegetarianForbidden
            : new HashSet<string>(StringComparer.OrdinalIgnoreCase);
}
