using MealPlannerPipeline.Core.Contracts;
using MealPlannerPipeline.Core.Pipeline;
using MealPlannerPipeline.Stages.MealPlannerStage;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace MealPlannerPipeline.Tests;

public class MealPlannerStageTests
{
    private readonly Mock<ILlmClient> _llmMock = new();
    private readonly MealPlannerStage _stage;
    private readonly PipelineInput _defaultInput = new("vegetarian", 600, 5, 30);

    public MealPlannerStageTests()
    {
        _stage = new MealPlannerStage(_llmMock.Object, NullLogger<MealPlannerStage>.Instance);
    }

    [Fact]
    public async Task ValidJson_ReturnsSuccess()
    {
        _llmMock.Setup(c => c.GenerateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidMealPlanJson());

        var result = await _stage.ExecuteAsync(_defaultInput);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(2, result.Value.Days.Count);
        Assert.Equal("Chickpea Curry", result.Value.Days[0].MealName);
    }

    [Fact]
    public async Task JsonWrappedInMarkdownFences_ParsesCorrectly()
    {
        var fenced = $"```json\n{ValidMealPlanJson()}\n```";
        _llmMock.Setup(c => c.GenerateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(fenced);

        var result = await _stage.ExecuteAsync(_defaultInput);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value!.Days.Count);
    }

    [Fact]
    public async Task InvalidJson_ReturnsFailure()
    {
        _llmMock.Setup(c => c.GenerateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("This is not JSON at all.");

        var result = await _stage.ExecuteAsync(_defaultInput);

        Assert.False(result.IsSuccess);
        Assert.Contains("JSON parse failed", result.ErrorMessage);
    }

    [Fact]
    public async Task LlmThrows_ReturnsFailure()
    {
        _llmMock.Setup(c => c.GenerateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Connection refused"));

        var result = await _stage.ExecuteAsync(_defaultInput);

        Assert.False(result.IsSuccess);
        Assert.Contains("LLM call failed", result.ErrorMessage);
    }

    [Fact]
    public async Task RawLlmOutput_StoredOnSuccess()
    {
        var raw = ValidMealPlanJson();
        _llmMock.Setup(c => c.GenerateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(raw);

        var result = await _stage.ExecuteAsync(_defaultInput);

        Assert.True(result.IsSuccess);
        Assert.Equal(raw, result.RawLlmOutput);
    }

    [Fact]
    public async Task RawLlmOutput_StoredOnParseFailure()
    {
        var raw = "not valid json";
        _llmMock.Setup(c => c.GenerateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(raw);

        var result = await _stage.ExecuteAsync(_defaultInput);

        Assert.False(result.IsSuccess);
        Assert.Equal(raw, result.RawLlmOutput);
    }

    private static string ValidMealPlanJson() => """
        {
          "days": [
            {
              "day": "Monday",
              "meal_name": "Chickpea Curry",
              "diet_tags": ["vegetarian"],
              "estimated_calories": 520
            },
            {
              "day": "Tuesday",
              "meal_name": "Lentil Soup",
              "diet_tags": ["vegetarian"],
              "estimated_calories": 480
            }
          ]
        }
        """;
}
