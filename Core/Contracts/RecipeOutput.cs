namespace MealPlannerPipeline.Core.Contracts;

public record RecipeOutput(
    string MealName,
    List<Ingredient> Ingredients,
    List<string> Steps,
    int EstimatedCalories
);

public record Ingredient(string Name, double Quantity, string Unit);
