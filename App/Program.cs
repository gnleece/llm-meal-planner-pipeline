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
Console.WriteLine($"Pipeline completed in {result.Duration.TotalSeconds:F1}s");
