namespace MealPlannerPipeline.Core.Contracts;

public record GroceryOutput(List<GroceryItem> Items);

public record GroceryItem(string Name, double TotalQuantity, string Unit);
