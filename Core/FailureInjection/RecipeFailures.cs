using MealPlannerPipeline.Core.Contracts;
using MealPlannerPipeline.Core.Pipeline;

namespace MealPlannerPipeline.Core.FailureInjection;

public static class RecipeFailures
{
    public static StageResult<RecipeOutput> Create(FailureMode mode, PlannedMeal meal, PipelineInput input) => mode switch
    {
        FailureMode.MalformedJson => MalformedJson(meal),
        FailureMode.DietaryViolation => DietaryViolation(meal),
        FailureMode.MissingIngredients => MissingIngredients(meal, input),
        FailureMode.ExceedCalories => ExceedCalories(meal, input),
        _ => throw new ArgumentOutOfRangeException(nameof(mode)),
    };

    private static StageResult<RecipeOutput> MalformedJson(PlannedMeal meal) =>
        StageResult<RecipeOutput>.Failure(
            $"JSON parse failed: unterminated string for {meal.MealName}",
            rawOutput: "{ \"meal_name\": \"unterminated",
            prompt: "[failure-injected]");

    private static StageResult<RecipeOutput> DietaryViolation(PlannedMeal meal) =>
        StageResult<RecipeOutput>.Success(
            new RecipeOutput(
                meal.MealName,
                [
                    new Ingredient("chicken breast", 6,   "oz"),
                    new Ingredient("olive oil",      1,   "tbsp"),
                    new Ingredient("garlic",         2,   "cloves"),
                    new Ingredient("lemon",          1,   "count"),
                ],
                [
                    "Season chicken with salt and pepper.",
                    "Heat olive oil in a pan.",
                    "Cook chicken 6 minutes per side until done.",
                    "Squeeze lemon over the top and serve.",
                ],
                meal.EstimatedCalories),
            rawOutput: "[failure-injected: dietary violation — chicken in vegetarian recipe]",
            prompt: "[failure-injected]");

    private static StageResult<RecipeOutput> MissingIngredients(PlannedMeal meal, PipelineInput input) =>
        StageResult<RecipeOutput>.Success(
            new RecipeOutput(
                meal.MealName,
                [],
                [
                    "Follow the recipe.",
                    "Serve hot.",
                ],
                meal.EstimatedCalories),
            rawOutput: "[failure-injected: empty ingredients list]",
            prompt: "[failure-injected]");

    private static StageResult<RecipeOutput> ExceedCalories(PlannedMeal meal, PipelineInput input) =>
        StageResult<RecipeOutput>.Success(
            new RecipeOutput(
                meal.MealName,
                [
                    new Ingredient("pasta",          3,   "cups"),
                    new Ingredient("heavy cream",    1,   "cup"),
                    new Ingredient("parmesan",       1,   "cup"),
                    new Ingredient("butter",         4,   "tbsp"),
                ],
                [
                    "Boil pasta until al dente.",
                    "Melt butter, add cream and parmesan.",
                    "Toss pasta in sauce.",
                    "Serve immediately.",
                ],
                input.MaxCalories + 300),
            rawOutput: "[failure-injected: calorie limit exceeded]",
            prompt: "[failure-injected]");
}
