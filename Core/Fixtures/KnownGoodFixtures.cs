using MealPlannerPipeline.Core.Contracts;

namespace MealPlannerPipeline.Core.Fixtures;

public static class KnownGoodFixtures
{
    public static MealPlanOutput MealPlan => new(
    [
        new PlannedMeal("Monday",   "Chickpea Spinach Curry",         ["vegetarian"], 520),
        new PlannedMeal("Tuesday",  "Black Bean Tacos",                ["vegetarian"], 480),
        new PlannedMeal("Wednesday","Lentil Vegetable Soup",           ["vegetarian"], 410),
        new PlannedMeal("Thursday", "Margherita Flatbread Pizza",      ["vegetarian"], 560),
        new PlannedMeal("Friday",   "Tofu Stir-Fry with Brown Rice",   ["vegetarian"], 500),
    ]);

    public static List<RecipeOutput> Recipes =>
    [
        new RecipeOutput(
            "Chickpea Spinach Curry",
            [
                new Ingredient("chickpeas",      2,   "cups"),
                new Ingredient("fresh spinach",  3,   "cups"),
                new Ingredient("diced tomatoes", 1,   "can"),
                new Ingredient("coconut milk",   0.5, "cup"),
                new Ingredient("curry powder",   2,   "tsp"),
                new Ingredient("olive oil",      1,   "tbsp"),
            ],
            [
                "Heat olive oil in a large pan over medium heat.",
                "Add curry powder and toast for 30 seconds.",
                "Add diced tomatoes and cook for 5 minutes.",
                "Stir in chickpeas and coconut milk; simmer 10 minutes.",
                "Fold in spinach and cook until wilted.",
                "Season with salt to taste and serve.",
            ],
            520),

        new RecipeOutput(
            "Black Bean Tacos",
            [
                new Ingredient("black beans",    1.5, "cups"),
                new Ingredient("corn tortillas", 4,   "count"),
                new Ingredient("avocado",        1,   "count"),
                new Ingredient("red onion",      0.5, "cup"),
                new Ingredient("lime juice",     2,   "tbsp"),
                new Ingredient("cumin",          1,   "tsp"),
            ],
            [
                "Drain and rinse black beans; warm in a saucepan with cumin.",
                "Dice avocado and toss with lime juice.",
                "Warm tortillas in a dry skillet.",
                "Fill tortillas with beans, avocado, and red onion.",
                "Serve immediately.",
            ],
            480),

        new RecipeOutput(
            "Lentil Vegetable Soup",
            [
                new Ingredient("green lentils",  1,   "cup"),
                new Ingredient("carrot",         2,   "count"),
                new Ingredient("celery stalks",  2,   "count"),
                new Ingredient("vegetable broth",4,   "cups"),
                new Ingredient("garlic cloves",  3,   "count"),
                new Ingredient("olive oil",      1,   "tbsp"),
            ],
            [
                "Sauté garlic, carrot, and celery in olive oil for 5 minutes.",
                "Add lentils and vegetable broth.",
                "Bring to a boil then simmer 25 minutes until lentils are tender.",
                "Season with salt, pepper, and herbs.",
                "Serve hot.",
            ],
            410),

        new RecipeOutput(
            "Margherita Flatbread Pizza",
            [
                new Ingredient("flatbread",      2,   "count"),
                new Ingredient("tomato sauce",   0.5, "cup"),
                new Ingredient("fresh mozzarella",4,  "oz"),
                new Ingredient("fresh basil",    0.25,"cup"),
                new Ingredient("olive oil",      1,   "tbsp"),
            ],
            [
                "Preheat oven to 425°F.",
                "Spread tomato sauce over flatbreads.",
                "Top with torn mozzarella.",
                "Bake 10 minutes until cheese is bubbly.",
                "Finish with fresh basil and a drizzle of olive oil.",
            ],
            560),

        new RecipeOutput(
            "Tofu Stir-Fry with Brown Rice",
            [
                new Ingredient("extra firm tofu",8,   "oz"),
                new Ingredient("brown rice",     1,   "cup"),
                new Ingredient("broccoli florets",2,  "cups"),
                new Ingredient("soy sauce",      3,   "tbsp"),
                new Ingredient("sesame oil",     1,   "tbsp"),
                new Ingredient("ginger",         1,   "tsp"),
            ],
            [
                "Cook brown rice per package directions.",
                "Press tofu dry and cube; pan-fry in sesame oil until golden.",
                "Add broccoli and stir-fry 4 minutes.",
                "Add soy sauce and ginger; toss to coat.",
                "Serve over brown rice.",
            ],
            500),
    ];
}
