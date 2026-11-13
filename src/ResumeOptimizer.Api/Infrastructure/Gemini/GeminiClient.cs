using System.Net;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Options;
using ResumeOptimizer.Api.Configuration;

namespace ResumeOptimizer.Api.Infrastructure.Gemini;

public class GeminiClient
{
    private readonly HttpClient _httpClient;
    private readonly GeminiOptions _options;

    public GeminiClient(HttpClient httpClient, IOptions<GeminiOptions> options)
    {
        _options = options.Value;
        _httpClient = httpClient;
        _httpClient.BaseAddress = new Uri(_options.BaseUrl);
        _httpClient.Timeout = TimeSpan.FromSeconds(120);
    }

    /// <summary>
    /// Calls the Gemini generateContent endpoint.
    /// When jsonMode is true, sets responseMimeType to application/json so Gemini
    /// guarantees the response is valid JSON — no markdown fences or prose preamble.
    /// </summary>
    public async Task<string> GenerateAsync(
        string userPrompt,
        string? systemPrompt = null,
        bool jsonMode = false,
        CancellationToken ct = default)
    {
        var url = $"/v1beta/models/{_options.GenerationModel}:generateContent?key={_options.ApiKey}";

        var requestBody = BuildGenerationRequest(userPrompt, systemPrompt, jsonMode);

        var response = await _httpClient.PostAsJsonAsync(url, requestBody, ct);
        await EnsureSuccessOrThrow(response, ct);

        var json = await response.Content.ReadFromJsonAsync<JsonNode>(cancellationToken: ct);
        return json?["candidates"]?[0]?["content"]?["parts"]?[0]?["text"]?.GetValue<string>()
            ?? throw new InvalidOperationException("Gemini returned no content in candidates.");
    }

    /// <summary>
    /// Calls the Gemini embedContent endpoint using gemini-embedding-001 (3072 dimensions).
    /// The Atlas Vector Search index must be configured for 3072 dimensions to match.
    /// </summary>
    public async Task<float[]> EmbedAsync(string text, CancellationToken ct = default)
    {
        var url = $"/v1beta/models/{_options.EmbeddingModel}:embedContent?key={_options.ApiKey}";

        var requestBody = new
        {
            content = new { parts = new[] { new { text } } }
        };

        var response = await _httpClient.PostAsJsonAsync(url, requestBody, ct);
        await EnsureSuccessOrThrow(response, ct);

        var json = await response.Content.ReadFromJsonAsync<JsonNode>(cancellationToken: ct);
        var values = json?["embedding"]?["values"]?.AsArray();
        if (values is null || values.Count == 0)
            throw new InvalidOperationException("Gemini returned no embedding values.");

        return values.Select(v => v!.GetValue<float>()).ToArray();
    }

    private static object BuildGenerationRequest(string userPrompt, string? systemPrompt, bool jsonMode)
    {
        // Anonymous types can't be conditionally extended, so build as dictionary
        var body = new Dictionary<string, object>
        {
            ["contents"] = new[]
            {
                new { role = "user", parts = new[] { new { text = userPrompt } } }
            }
        };

        if (systemPrompt is not null)
            body["system_instruction"] = new { parts = new[] { new { text = systemPrompt } } };

        if (jsonMode)
            body["generationConfig"] = new { responseMimeType = "application/json" };

        return body;
    }

    /// <summary>
    /// Reads the response body before throwing so the error message includes the Gemini error detail
    /// (e.g. quota exceeded, invalid API key) rather than a generic status code.
    /// </summary>
    private static async Task EnsureSuccessOrThrow(HttpResponseMessage response, CancellationToken ct)
    {
        if (response.IsSuccessStatusCode) return;

        var body = await response.Content.ReadAsStringAsync(ct);
        throw new HttpRequestException(
            $"Gemini API error {(int)response.StatusCode}: {body}",
            inner: null,
            statusCode: response.StatusCode);
    }
}
