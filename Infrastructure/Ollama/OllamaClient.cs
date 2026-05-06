using System.Text;
using System.Text.Json;
using MealPlannerPipeline.Core.Pipeline;
using Microsoft.Extensions.Logging;

namespace MealPlannerPipeline.Infrastructure.Ollama;

public class OllamaClient : ILlmClient
{
    private readonly HttpClient _http;
    private readonly string _model;
    private readonly ILogger<OllamaClient> _logger;

    public OllamaClient(HttpClient http, string model, ILogger<OllamaClient> logger)
    {
        _http = http;
        _model = model;
        _logger = logger;
    }

    public async Task<string> GenerateAsync(string prompt, CancellationToken cancellationToken = default)
    {
        var requestBody = new { model = _model, prompt, stream = false };
        var json = JsonSerializer.Serialize(requestBody);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");

        _logger.LogDebug("Sending prompt to Ollama (model={Model}, promptLength={Len})", _model, prompt.Length);

        var response = await _http.PostAsync("/api/generate", content, cancellationToken);
        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);

        using var doc = JsonDocument.Parse(responseJson);
        return doc.RootElement.GetProperty("response").GetString()
               ?? throw new InvalidOperationException("Ollama returned an empty response field.");
    }
}
