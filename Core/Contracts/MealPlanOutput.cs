namespace MealPlannerPipeline.Core.Contracts;

public record MealPlanOutput(List<PlannedMeal> Days);

public record PlannedMeal(
    string Day,
    string MealName,
    List<string> DietTags,
    int EstimatedCalories
);
