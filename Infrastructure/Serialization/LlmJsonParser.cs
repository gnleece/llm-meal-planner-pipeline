namespace MealPlannerPipeline.Infrastructure.Serialization;

public static class LlmJsonParser
{
    /// <summary>
    /// Strips markdown code fences that LLMs commonly wrap JSON output in.
    /// </summary>
    public static string SanitizeJson(string raw)
    {
        var trimmed = raw.Trim();

        if (!trimmed.StartsWith("```"))
            return trimmed;

        var firstNewline = trimmed.IndexOf('\n');
        var lastFence = trimmed.LastIndexOf("```");

        if (firstNewline > 0 && lastFence > firstNewline)
            return trimmed[(firstNewline + 1)..lastFence].Trim();

        return trimmed;
    }
}
