using MealPlannerPipeline.Infrastructure.Serialization;

namespace MealPlannerPipeline.Tests;

public class LlmJsonParserTests
{
    [Fact]
    public void SanitizeJson_NoFences_ReturnsUnchanged()
    {
        var input = """{"key": "value"}""";
        Assert.Equal(input, LlmJsonParser.SanitizeJson(input));
    }

    [Fact]
    public void SanitizeJson_WithGenericFences_StripsFences()
    {
        var input = "```\n{\"key\": \"value\"}\n```";
        Assert.Equal("{\"key\": \"value\"}", LlmJsonParser.SanitizeJson(input));
    }

    [Fact]
    public void SanitizeJson_WithJsonLanguageTag_StripsFences()
    {
        var input = "```json\n{\"key\": \"value\"}\n```";
        Assert.Equal("{\"key\": \"value\"}", LlmJsonParser.SanitizeJson(input));
    }

    [Fact]
    public void SanitizeJson_WithLeadingAndTrailingWhitespace_Trims()
    {
        var input = "   {\"key\": \"value\"}   ";
        Assert.Equal("{\"key\": \"value\"}", LlmJsonParser.SanitizeJson(input));
    }

    [Fact]
    public void SanitizeJson_WithFencesAndWhitespace_StripsBoth()
    {
        var input = "  ```json\n{\"key\": \"value\"}\n```  ";
        Assert.Equal("{\"key\": \"value\"}", LlmJsonParser.SanitizeJson(input));
    }

    [Fact]
    public void SanitizeJson_MultilineJson_PreservesContent()
    {
        var json = "{\n  \"days\": []\n}";
        var input = $"```json\n{json}\n```";
        Assert.Equal(json, LlmJsonParser.SanitizeJson(input));
    }
}
