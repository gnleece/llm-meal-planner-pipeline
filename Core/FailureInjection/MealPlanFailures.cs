using MealPlannerPipeline.Core.Contracts;
using MealPlannerPipeline.Core.Pipeline;

namespace MealPlannerPipeline.Core.FailureInjection;

public static class MealPlanFailures
{
    public static StageResult<MealPlanOutput> Create(FailureMode mode, PipelineInput input) => mode switch
    {
        FailureMode.MalformedJson => MalformedJson(),
        FailureMode.DietaryViolation => DietaryViolation(input),
        FailureMode.ExceedCalories => ExceedCalories(input),
        FailureMode.MissingIngredients => DietaryViolation(input), // no direct analogue at plan level
        _ => throw new ArgumentOutOfRangeException(nameof(mode)),
    };

    private static StageResult<MealPlanOutput> MalformedJson() =>
        StageResult<MealPlanOutput>.Failure(
            "JSON parse failed: unexpected character '{' at position 12",
            rawOutput: "{ \"days\": [ { invalid json } ] }",
            prompt: "[failure-injected]");

    private static StageResult<MealPlanOutput> DietaryViolation(PipelineInput input) =>
        StageResult<MealPlanOutput>.Success(
            new MealPlanOutput(
            [
                new PlannedMeal("Monday",    "Grilled Chicken Breast", ["meat"],   420),
                new PlannedMeal("Tuesday",   "Beef Tacos",             ["meat"],   580),
                new PlannedMeal("Wednesday", "Pork Fried Rice",        ["meat"],   530),
                new PlannedMeal("Thursday",  "Salmon with Asparagus",  ["fish"],   490),
                new PlannedMeal("Friday",    "BLT Sandwich",           ["meat"],   460),
            ]),
            rawOutput: "[failure-injected: dietary violation — non-vegetarian meals]",
            prompt: "[failure-injected]");

    private static StageResult<MealPlanOutput> ExceedCalories(PipelineInput input) =>
        StageResult<MealPlanOutput>.Success(
            new MealPlanOutput(
            [
                new PlannedMeal("Monday",    "Loaded Nachos",                 [input.Diet], input.MaxCalories + 250),
                new PlannedMeal("Tuesday",   "Cheese Pizza (whole)",          [input.Diet], input.MaxCalories + 400),
                new PlannedMeal("Wednesday", "Pasta Carbonara (double size)", [input.Diet], input.MaxCalories + 180),
                new PlannedMeal("Thursday",  "Fried Rice with Egg",           [input.Diet], input.MaxCalories + 320),
                new PlannedMeal("Friday",    "Burrito Bowl (large)",          [input.Diet], input.MaxCalories + 150),
            ]),
            rawOutput: "[failure-injected: calorie limit exceeded]",
            prompt: "[failure-injected]");
}
