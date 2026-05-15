using System.Diagnostics;
using System.Text.Json;
using MealPlannerPipeline.Core.Contracts;
using MealPlannerPipeline.Core.Evals;
using MealPlannerPipeline.Core.FailureInjection;
using MealPlannerPipeline.Core.Traces;
using Microsoft.Extensions.Logging;

namespace MealPlannerPipeline.Core.Pipeline;

public class MealPlannerOrchestrator
{
    private readonly IPipelineStage<PipelineInput, MealPlanOutput> _mealPlanner;
    private readonly IPipelineStage<PlannedMeal, RecipeOutput> _recipeGenerator;
    private readonly IPipelineStage<List<RecipeOutput>, GroceryOutput> _groceryAggregator;
    private readonly ILogger<MealPlannerOrchestrator> _logger;

    private static readonly JsonSerializerOptions _traceJsonOptions = new()
    {
        WriteIndented = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

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
        PipelineInput input,
        PipelineOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        options ??= PipelineOptions.Default;

        var runId = DateTimeOffset.UtcNow.ToString("yyyyMMdd_HHmmss");
        var startedAt = DateTimeOffset.UtcNow;
        var stageTraces = new List<StageTrace>();

        _logger.LogInformation("=== Pipeline Run {RunId} Started ===", runId);
        var totalSw = Stopwatch.StartNew();

        // ── Stage 1: Meal Planner ──────────────────────────────────────────
        StageResult<MealPlanOutput> mealPlanResult;
        bool mealPlanInjected = false;
        bool mealPlanFailureInjected = false;

        if (options.KnownGoodMealPlan is not null)
        {
            _logger.LogInformation("[{Stage}] Bypassed — using known-good meal plan", _mealPlanner.StageName);
            mealPlanResult = StageResult<MealPlanOutput>.Success(options.KnownGoodMealPlan, "[known-good]");
            mealPlanInjected = true;
        }
        else if (options.InjectMealPlannerFailure.HasValue)
        {
            _logger.LogWarning("[{Stage}] Injecting failure: {Mode}", _mealPlanner.StageName, options.InjectMealPlannerFailure.Value);
            mealPlanResult = MealPlanFailures.Create(options.InjectMealPlannerFailure.Value, input);
            mealPlanFailureInjected = true;
        }
        else
        {
            var sw = Stopwatch.StartNew();
            mealPlanResult = await _mealPlanner.ExecuteAsync(input, cancellationToken);
            sw.Stop();
            _logger.LogDebug("[{Stage}] Latency: {Ms}ms", _mealPlanner.StageName, sw.ElapsedMilliseconds);
        }

        EvalResult? mealPlanEval = null;
        if (mealPlanResult.IsSuccess)
        {
            mealPlanEval = await new MealPlanEvaluator(input).EvaluateAsync(mealPlanResult.Value!);
            LogEvalResult(_mealPlanner.StageName, mealPlanEval);
        }

        stageTraces.Add(BuildTrace(
            _mealPlanner.StageName,
            Serialize(input),
            mealPlanResult,
            mealPlanEval,
            injected: mealPlanInjected,
            failureInjected: mealPlanFailureInjected));

        if (!mealPlanResult.IsSuccess)
        {
            _logger.LogError("Pipeline halted at {Stage}: {Error}", _mealPlanner.StageName, mealPlanResult.ErrorMessage);
            return PipelineRunResult.Failed(
                _mealPlanner.StageName, mealPlanResult.ErrorMessage!,
                BuildPipelineTrace(runId, startedAt, totalSw.Elapsed, stageTraces));
        }

        // ── Stage 2: Recipe Generator (one per meal) ──────────────────────
        var recipes = new List<RecipeOutput>();
        var recipeEvals = new List<EvalResult>();
        var recipeEvaluator = new RecipeEvaluator(input);

        foreach (var meal in mealPlanResult.Value!.Days)
        {
            StageResult<RecipeOutput> recipeResult;
            bool recipeInjected = false;
            bool recipeFailureInjected = false;

            if (options.KnownGoodRecipes is not null)
            {
                var match = options.KnownGoodRecipes.FirstOrDefault(r =>
                    r.MealName.Equals(meal.MealName, StringComparison.OrdinalIgnoreCase));

                if (match is not null)
                {
                    _logger.LogInformation("[{Stage}] Bypassed for {Meal} — using known-good recipe",
                        _recipeGenerator.StageName, meal.MealName);
                    recipeResult = StageResult<RecipeOutput>.Success(match, "[known-good]");
                    recipeInjected = true;
                }
                else
                {
                    _logger.LogWarning("[{Stage}] No known-good recipe for '{Meal}' — calling LLM",
                        _recipeGenerator.StageName, meal.MealName);
                    recipeResult = await _recipeGenerator.ExecuteAsync(meal, cancellationToken);
                }
            }
            else if (options.InjectRecipeFailure.HasValue)
            {
                _logger.LogWarning("[{Stage}] Injecting failure for {Meal}: {Mode}",
                    _recipeGenerator.StageName, meal.MealName, options.InjectRecipeFailure.Value);
                recipeResult = RecipeFailures.Create(options.InjectRecipeFailure.Value, meal, input);
                recipeFailureInjected = true;
            }
            else
            {
                recipeResult = await _recipeGenerator.ExecuteAsync(meal, cancellationToken);
            }

            EvalResult? recipeEval = null;
            if (recipeResult.IsSuccess)
            {
                recipeEval = await recipeEvaluator.EvaluateAsync(recipeResult.Value!);
                LogEvalResult($"{_recipeGenerator.StageName}[{meal.MealName}]", recipeEval);
                recipes.Add(recipeResult.Value!);
                recipeEvals.Add(recipeEval);
            }
            else
            {
                _logger.LogWarning("Skipping failed recipe for {Meal}: {Error}",
                    meal.MealName, recipeResult.ErrorMessage);
            }

            stageTraces.Add(BuildTrace(
                $"{_recipeGenerator.StageName}[{meal.MealName}]",
                Serialize(meal),
                recipeResult,
                recipeEval,
                injected: recipeInjected,
                failureInjected: recipeFailureInjected));
        }

        if (recipes.Count == 0)
        {
            var err = "All recipe generations failed.";
            return PipelineRunResult.Failed(_recipeGenerator.StageName, err,
                BuildPipelineTrace(runId, startedAt, totalSw.Elapsed, stageTraces));
        }

        if (recipes.Count < mealPlanResult.Value.Days.Count)
            _logger.LogWarning("Continuing with {Count}/{Total} recipes — some failed",
                recipes.Count, mealPlanResult.Value.Days.Count);

        // ── Stage 3: Grocery Aggregator ───────────────────────────────────
        var groceryResult = await _groceryAggregator.ExecuteAsync(recipes, cancellationToken);

        EvalResult? groceryEval = null;
        if (groceryResult.IsSuccess)
        {
            groceryEval = await new GroceryEvaluator(recipes).EvaluateAsync(groceryResult.Value!);
            LogEvalResult(_groceryAggregator.StageName, groceryEval);
        }

        stageTraces.Add(BuildTrace(
            _groceryAggregator.StageName,
            Serialize(recipes),
            groceryResult,
            groceryEval));

        if (!groceryResult.IsSuccess)
        {
            _logger.LogError("Pipeline halted at {Stage}: {Error}", _groceryAggregator.StageName, groceryResult.ErrorMessage);
            return PipelineRunResult.Failed(_groceryAggregator.StageName, groceryResult.ErrorMessage!,
                BuildPipelineTrace(runId, startedAt, totalSw.Elapsed, stageTraces));
        }

        var endToEndEval = new EndToEndEvaluator(input)
            .Evaluate(mealPlanResult.Value!, recipes, groceryResult.Value!);
        LogEvalResult("EndToEnd", endToEndEval);

        totalSw.Stop();
        _logger.LogInformation("=== Pipeline Run {RunId} Completed in {Ms}ms ===", runId, totalSw.ElapsedMilliseconds);

        var trace = BuildPipelineTrace(runId, startedAt, totalSw.Elapsed, stageTraces);

        return PipelineRunResult.Success(
            mealPlanResult.Value!, recipes, groceryResult.Value!,
            mealPlanEval!, recipeEvals, groceryEval!, endToEndEval,
            totalSw.Elapsed, trace);
    }

    private void LogEvalResult(string stage, EvalResult eval)
    {
        var status = eval.Passed ? "PASS" : "FAIL";
        _logger.LogInformation("[Eval] {Stage}: {Status} (score={Score:F2})", stage, status, eval.Score);
        foreach (var issue in eval.Issues)
            _logger.LogWarning("[Eval] {Stage} issue: {Issue}", stage, issue);
    }

    private static StageTrace BuildTrace<T>(
        string stageName,
        string inputJson,
        StageResult<T> result,
        EvalResult? eval,
        bool injected = false,
        bool failureInjected = false) =>
        new()
        {
            StageName = stageName,
            InputJson = inputJson,
            Prompt = result.Prompt,
            RawLlmOutput = result.RawLlmOutput,
            OutputJson = result.IsSuccess ? Serialize(result.Value) : null,
            Eval = eval,
            Latency = TimeSpan.Zero, // latency is tracked at the orchestrator level per-stage
            IsSuccess = result.IsSuccess,
            ErrorMessage = result.ErrorMessage,
            WasInjected = injected,
            WasFailureInjected = failureInjected,
        };

    private static PipelineTrace BuildPipelineTrace(
        string runId,
        DateTimeOffset startedAt,
        TimeSpan totalDuration,
        List<StageTrace> stages) =>
        new()
        {
            RunId = runId,
            StartedAt = startedAt,
            TotalDuration = totalDuration,
            Stages = stages,
        };

    private static string Serialize<T>(T value) =>
        JsonSerializer.Serialize(value, _traceJsonOptions);
}
