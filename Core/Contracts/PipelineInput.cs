namespace MealPlannerPipeline.Core.Contracts;

public record PipelineInput(
    string Diet,
    int MaxCalories,
    int Days,
    int MaxPrepTimeMinutes
);
