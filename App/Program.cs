using System.Text.Json;
using MealPlannerPipeline.Core.Contracts;
using MealPlannerPipeline.Core.FailureInjection;
using MealPlannerPipeline.Core.Fixtures;
using MealPlannerPipeline.Core.Pipeline;
using MealPlannerPipeline.Core.Traces;
using MealPlannerPipeline.Infrastructure.Ollama;
using MealPlannerPipeline.Infrastructure.Serialization;
using MealPlannerPipeline.Stages.GroceryAggregatorStage;
using MealPlannerPipeline.Stages.MealPlannerStage;
using MealPlannerPipeline.Stages.RecipeGeneratorStage;
using Microsoft.Extensions.Logging;

// ── Parse CLI arguments ───────────────────────────────────────────────────────
var options = ParseOptions(args);

// ── Wiring ────────────────────────────────────────────────────────────────────
using var loggerFactory = LoggerFactory.Create(builder =>
    builder.AddConsole().SetMinimumLevel(LogLevel.Information));

var httpClient = new HttpClient { BaseAddress = new Uri("http://localhost:11434") };
var ollamaClient = new OllamaClient(
    httpClient,
    model: "llama3",
    loggerFactory.CreateLogger<OllamaClient>());

var mealPlanner    = new MealPlannerStage(ollamaClient, loggerFactory.CreateLogger<MealPlannerStage>());
var recipeGenerator = new RecipeGeneratorStage(ollamaClient, loggerFactory.CreateLogger<RecipeGeneratorStage>());
var groceryAggregator = new GroceryAggregatorStage(loggerFactory.CreateLogger<GroceryAggregatorStage>());

var orchestrator = new MealPlannerOrchestrator(
    mealPlanner, recipeGenerator, groceryAggregator,
    loggerFactory.CreateLogger<MealPlannerOrchestrator>());

var input = new PipelineInput(
    Diet: "vegetarian",
    MaxCalories: 600,
    Days: 5,
    MaxPrepTimeMinutes: 30);

// ── Print header ──────────────────────────────────────────────────────────────
Console.WriteLine("=== LLM Meal Planner Pipeline ===");
Console.WriteLine($"Input: {JsonSerializer.Serialize(input, JsonOptions.Default)}");

if (options.KnownGoodMealPlan is not null)
    Console.WriteLine("[Override] MealPlanner stage bypassed — using known-good meal plan");
if (options.KnownGoodRecipes is not null)
    Console.WriteLine("[Override] RecipeGenerator stage bypassed — using known-good recipes");
if (options.InjectMealPlannerFailure.HasValue)
    Console.WriteLine($"[Inject]   MealPlanner failure: {options.InjectMealPlannerFailure.Value}");
if (options.InjectRecipeFailure.HasValue)
    Console.WriteLine($"[Inject]   Recipe failure: {options.InjectRecipeFailure.Value}");

Console.WriteLine();

// ── Run pipeline ──────────────────────────────────────────────────────────────
var result = await orchestrator.RunAsync(input, options);

// ── Results ───────────────────────────────────────────────────────────────────
if (!result.IsSuccess)
{
    Console.WriteLine($"Pipeline FAILED at stage '{result.FailedStage}': {result.ErrorMessage}");
    PrintTraceSummary(result.Trace);
    SaveTrace(result.Trace);
    return;
}

Console.WriteLine("=== MEAL PLAN ===");
foreach (var day in result.MealPlan!.Days)
    Console.WriteLine($"  {day.Day}: {day.MealName} (~{day.EstimatedCalories} cal)");

Console.WriteLine();
Console.WriteLine("=== RECIPES ===");
foreach (var recipe in result.Recipes!)
{
    Console.WriteLine($"  {recipe.MealName}");
    Console.WriteLine($"    Ingredients: {recipe.Ingredients.Count}");
    Console.WriteLine($"    Steps: {recipe.Steps.Count}");
    Console.WriteLine($"    Calories: ~{recipe.EstimatedCalories}");
}

Console.WriteLine();
Console.WriteLine("=== GROCERY LIST ===");
foreach (var item in result.GroceryList!.Items)
    Console.WriteLine($"  {item.TotalQuantity} {item.Unit} {item.Name}");

Console.WriteLine();
Console.WriteLine("=== EVAL SUMMARY ===");
PrintEval("Meal Plan    ", result.MealPlanEval);
if (result.RecipeEvals != null)
    for (int i = 0; i < result.RecipeEvals.Count; i++)
        PrintEval($"Recipe [{result.Recipes![i].MealName}]", result.RecipeEvals[i]);
PrintEval("Grocery List ", result.GroceryEval);
PrintEval("End-to-End   ", result.EndToEndEval);

Console.WriteLine();
PrintTraceSummary(result.Trace);
SaveTrace(result.Trace);

Console.WriteLine($"Pipeline completed in {result.Duration.TotalSeconds:F1}s");

// ── Helpers ───────────────────────────────────────────────────────────────────
static void PrintEval(string label, EvalResult? eval)
{
    if (eval is null) return;
    var status = eval.Passed ? "PASS" : "FAIL";
    Console.WriteLine($"  {label}: {status} (score={eval.Score:P0})");
    foreach (var issue in eval.Issues)
        Console.WriteLine($"    ! {issue}");
}

static void PrintTraceSummary(PipelineTrace? trace)
{
    if (trace is null) return;

    Console.WriteLine("=== PIPELINE TRACE ===");
    Console.WriteLine($"  Run ID   : {trace.RunId}");
    Console.WriteLine($"  Started  : {trace.StartedAt:yyyy-MM-dd HH:mm:ss} UTC");
    Console.WriteLine($"  Duration : {trace.TotalDuration.TotalSeconds:F1}s");
    Console.WriteLine();
    Console.WriteLine("  Stages:");

    foreach (var stage in trace.Stages)
    {
        var status   = stage.IsSuccess ? "OK  " : "FAIL";
        var tag      = stage.WasFailureInjected ? " [failure-injected]"
                     : stage.WasInjected        ? " [known-good]"
                     : string.Empty;
        var evalPart = stage.Eval is not null
                     ? $" | eval={(stage.Eval.Passed ? "PASS" : "FAIL")} score={stage.Eval.Score:F2}"
                     : string.Empty;

        Console.WriteLine($"    [{status}] {stage.StageName}{tag}{evalPart}");

        if (!stage.IsSuccess)
            Console.WriteLine($"           Error: {stage.ErrorMessage}");

        if (stage.Eval is not null)
            foreach (var issue in stage.Eval.Issues)
                Console.WriteLine($"           ! {issue}");
    }

    Console.WriteLine();
}

static void SaveTrace(PipelineTrace? trace)
{
    if (trace is null) return;

    try
    {
        var dir = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "runs");
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, $"{trace.RunId}.json");
        var json = JsonSerializer.Serialize(trace, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        });
        File.WriteAllText(path, json);
        Console.WriteLine($"  Trace saved to: {Path.GetFullPath(path)}");
        Console.WriteLine();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"  [warn] Could not save trace: {ex.Message}");
    }
}

static PipelineOptions ParseOptions(string[] args)
{
    MealPlanOutput? knownGoodMealPlan = null;
    List<RecipeOutput>? knownGoodRecipes = null;
    FailureMode? injectMealPlanFailure = null;
    FailureMode? injectRecipeFailure = null;

    foreach (var arg in args)
    {
        if (arg == "--use-known-good-meal-plan")
        {
            knownGoodMealPlan = KnownGoodFixtures.MealPlan;
        }
        else if (arg == "--use-known-good-recipes")
        {
            knownGoodRecipes = KnownGoodFixtures.Recipes;
        }
        else if (arg.StartsWith("--inject-meal-plan-failure="))
        {
            injectMealPlanFailure = ParseFailureMode(arg["--inject-meal-plan-failure=".Length..]);
        }
        else if (arg.StartsWith("--inject-recipe-failure="))
        {
            injectRecipeFailure = ParseFailureMode(arg["--inject-recipe-failure=".Length..]);
        }
        else if (arg == "--help" || arg == "-h")
        {
            Console.WriteLine("""
                Usage: MealPlannerPipeline.App [options]

                Options:
                  --use-known-good-meal-plan              Bypass MealPlanner with hardcoded data
                  --use-known-good-recipes                Bypass RecipeGenerator with hardcoded data
                  --inject-meal-plan-failure=<mode>       Inject a meal plan failure
                  --inject-recipe-failure=<mode>          Inject a recipe failure

                Failure modes: malformed-json | dietary-violation | missing-ingredients | exceed-calories

                Examples:
                  dotnet run                                             Normal run
                  dotnet run -- --use-known-good-recipes                Skip LLM recipe calls
                  dotnet run -- --inject-recipe-failure=dietary-violation  Test that evals catch meat in vegetarian recipes
                """);
            Environment.Exit(0);
        }
    }

    return new PipelineOptions
    {
        KnownGoodMealPlan = knownGoodMealPlan,
        KnownGoodRecipes = knownGoodRecipes,
        InjectMealPlannerFailure = injectMealPlanFailure,
        InjectRecipeFailure = injectRecipeFailure,
    };
}

static FailureMode ParseFailureMode(string value) => value.ToLowerInvariant() switch
{
    "malformed-json"       => FailureMode.MalformedJson,
    "dietary-violation"    => FailureMode.DietaryViolation,
    "missing-ingredients"  => FailureMode.MissingIngredients,
    "exceed-calories"      => FailureMode.ExceedCalories,
    _ => throw new ArgumentException($"Unknown failure mode: '{value}'. Valid: malformed-json, dietary-violation, missing-ingredients, exceed-calories"),
};
