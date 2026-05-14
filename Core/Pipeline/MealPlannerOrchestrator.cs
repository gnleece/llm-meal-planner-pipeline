using System.Diagnostics;
using MealPlannerPipeline.Core.Contracts;
using MealPlannerPipeline.Core.Evals;
using Microsoft.Extensions.Logging;

namespace MealPlannerPipeline.Core.Pipeline;

public class MealPlannerOrchestrator
{
    private readonly IPipelineStage<PipelineInput, MealPlanOutput> _mealPlanner;
    private readonly IPipelineStage<PlannedMeal, RecipeOutput> _recipeGenerator;
    private readonly IPipelineStage<List<RecipeOutput>, GroceryOutput> _groceryAggregator;
    private readonly ILogger<MealPlannerOrchestrator> _logger;

    public MealPlannerOrchestrator(
        IPipelineStage<PipelineInput, MealPlanOutput> mealPlanner,
        IPipelineStage<PlannedMeal, RecipeOutput> recipeGenerator,
        IPipelineStage<List<RecipeOutput>, GroceryOutput> groceryAggregator,
        ILogger<MealPlannerOrchestrator> logger)
    {
        _mealPlanner = mealPlanner;
        _recipeGenerator = recipeGenerator;
        _groceryAggregator = groceryAggregator;
        _logger = logger;
    }

    public async Task<PipelineRunResult> RunAsync(
        PipelineInput input, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("=== Pipeline Run Started ===");
        var sw = Stopwatch.StartNew();

        // Stage 1 — meal plan
        var mealPlanResult = await _mealPlanner.ExecuteAsync(input, cancellationToken);
        if (!mealPlanResult.IsSuccess)
        {
            _logger.LogError("Pipeline halted at {Stage}: {Error}",
                _mealPlanner.StageName, mealPlanResult.ErrorMessage);
            return PipelineRunResult.Failed(_mealPlanner.StageName, mealPlanResult.ErrorMessage!);
        }

        var mealPlanEval = await new MealPlanEvaluator(input).EvaluateAsync(mealPlanResult.Value!);
        LogEvalResult(_mealPlanner.StageName, mealPlanEval);

        // Stage 2 — one recipe per meal; skip failed recipes rather than aborting the whole run
        // Phase 4 will replace this skip-and-continue with eval-driven retries
        var recipes = new List<RecipeOutput>();
        var recipeEvals = new List<EvalResult>();
        var recipeEvaluator = new RecipeEvaluator(input);

        foreach (var meal in mealPlanResult.Value!.Days)
        {
            var recipeResult = await _recipeGenerator.ExecuteAsync(meal, cancellationToken);
            if (!recipeResult.IsSuccess)
            {
                _logger.LogWarning("Skipping failed recipe for {Meal}: {Error}",
                    meal.MealName, recipeResult.ErrorMessage);
                continue;
            }

            var recipeEval = await recipeEvaluator.EvaluateAsync(recipeResult.Value!);
            LogEvalResult($"{_recipeGenerator.StageName}[{meal.MealName}]", recipeEval);

            recipes.Add(recipeResult.Value!);
            recipeEvals.Add(recipeEval);
        }

        if (recipes.Count == 0)
        {
            return PipelineRunResult.Failed(_recipeGenerator.StageName, "All recipe generations failed.");
        }

        if (recipes.Count < mealPlanResult.Value.Days.Count)
        {
            _logger.LogWarning("Continuing with {Count}/{Total} recipes — some failed",
                recipes.Count, mealPlanResult.Value.Days.Count);
        }

        // Stage 3 — aggregate grocery list
        var groceryResult = await _groceryAggregator.ExecuteAsync(recipes, cancellationToken);
        if (!groceryResult.IsSuccess)
        {
            _logger.LogError("Pipeline halted at {Stage}: {Error}",
                _groceryAggregator.StageName, groceryResult.ErrorMessage);
            return PipelineRunResult.Failed(_groceryAggregator.StageName, groceryResult.ErrorMessage!);
        }

        var groceryEval = await new GroceryEvaluator(recipes).EvaluateAsync(groceryResult.Value!);
        LogEvalResult(_groceryAggregator.StageName, groceryEval);

        var endToEndEval = new EndToEndEvaluator(input)
            .Evaluate(mealPlanResult.Value!, recipes, groceryResult.Value!);
        LogEvalResult("EndToEnd", endToEndEval);

        sw.Stop();
        _logger.LogInformation("=== Pipeline Run Completed in {Ms}ms ===", sw.ElapsedMilliseconds);

        return PipelineRunResult.Success(
            mealPlanResult.Value!, recipes, groceryResult.Value!,
            mealPlanEval, recipeEvals, groceryEval, endToEndEval,
            sw.Elapsed);
    }

    private void LogEvalResult(string stage, EvalResult eval)
    {
        var status = eval.Passed ? "PASS" : "FAIL";
        _logger.LogInformation("[Eval] {Stage}: {Status} (score={Score:F2})", stage, status, eval.Score);
        foreach (var issue in eval.Issues)
            _logger.LogWarning("[Eval] {Stage} issue: {Issue}", stage, issue);
    }
}
