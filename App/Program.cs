using System.Text.Json;
using MealPlannerPipeline.Core.Contracts;
using MealPlannerPipeline.Core.Pipeline;
using MealPlannerPipeline.Infrastructure.Ollama;
using MealPlannerPipeline.Infrastructure.Serialization;
using MealPlannerPipeline.Stages.GroceryAggregatorStage;
using MealPlannerPipeline.Stages.MealPlannerStage;
using MealPlannerPipeline.Stages.RecipeGeneratorStage;
using Microsoft.Extensions.Logging;

using var loggerFactory = LoggerFactory.Create(builder =>
    builder.AddConsole().SetMinimumLevel(LogLevel.Information));

var httpClient = new HttpClient { BaseAddress = new Uri("http://localhost:11434") };
var ollamaClient = new OllamaClient(
    httpClient,
    model: "llama3",
    loggerFactory.CreateLogger<OllamaClient>());

var mealPlanner = new MealPlannerStage(ollamaClient, loggerFactory.CreateLogger<MealPlannerStage>());
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

Console.WriteLine("=== LLM Meal Planner Pipeline ===");
Console.WriteLine($"Input: {JsonSerializer.Serialize(input, JsonOptions.Default)}");
Console.WriteLine();

var result = await orchestrator.RunAsync(input);

if (!result.IsSuccess)
{
    Console.WriteLine($"Pipeline FAILED at stage '{result.FailedStage}': {result.ErrorMessage}");
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
Console.WriteLine($"Pipeline completed in {result.Duration.TotalSeconds:F1}s");

static void PrintEval(string label, EvalResult? eval)
{
    if (eval is null) return;
    var status = eval.Passed ? "PASS" : "FAIL";
    Console.WriteLine($"  {label}: {status} (score={eval.Score:P0})");
    foreach (var issue in eval.Issues)
        Console.WriteLine($"    ! {issue}");
}
