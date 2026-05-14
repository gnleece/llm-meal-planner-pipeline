using MealPlannerPipeline.Core.Contracts;
using MealPlannerPipeline.Core.Pipeline;

namespace MealPlannerPipeline.Core.Evals;

public class MealPlanEvaluator : IEvaluator<MealPlanOutput>
{
    private readonly PipelineInput _constraints;

    public MealPlanEvaluator(PipelineInput constraints)
    {
        _constraints = constraints;
    }

    public Task<EvalResult> EvaluateAsync(MealPlanOutput plan)
    {
        var issues = new List<string>();
        int passed = 0;
        const int checks = 4;

        // Check 1: correct meal count
        if (plan.Days.Count == _constraints.Days)
            passed++;
        else
            issues.Add($"Expected {_constraints.Days} meals, got {plan.Days.Count}.");

        // Check 2: calorie limits respected
        var calorieViolations = plan.Days.Where(m => m.EstimatedCalories > _constraints.MaxCalories).ToList();
        if (calorieViolations.Count == 0)
            passed++;
        else
            foreach (var m in calorieViolations)
                issues.Add($"'{m.MealName}' exceeds calorie limit: {m.EstimatedCalories} > {_constraints.MaxCalories}.");

        // Check 3: dietary tags present on every meal
        var tagViolations = plan.Days
            .Where(m => !m.DietTags.Any(t => t.Equals(_constraints.Diet, StringComparison.OrdinalIgnoreCase)))
            .ToList();
        if (tagViolations.Count == 0)
            passed++;
        else
            foreach (var m in tagViolations)
                issues.Add($"'{m.MealName}' is missing required diet tag '{_constraints.Diet}'.");

        // Check 4: no duplicate meal names
        var names = plan.Days.Select(m => m.MealName).ToList();
        if (names.Distinct(StringComparer.OrdinalIgnoreCase).Count() == names.Count)
            passed++;
        else
            issues.Add("Duplicate meal names detected in the plan.");

        return Task.FromResult(new EvalResult
        {
            Passed = issues.Count == 0,
            Score = (double)passed / checks,
            Issues = issues
        });
    }
}
