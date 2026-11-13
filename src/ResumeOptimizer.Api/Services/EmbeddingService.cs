using Microsoft.Extensions.Options;
using Polly;
using ResumeOptimizer.Api.Configuration;
using ResumeOptimizer.Api.Infrastructure.Gemini;
using ResumeOptimizer.Api.Infrastructure.Resilience;
using ResumeOptimizer.Api.Services.Interfaces;

namespace ResumeOptimizer.Api.Services;

public class EmbeddingService : IEmbeddingService
{
    private readonly GeminiClient _geminiClient;
    private readonly ResiliencePipeline _retryPipeline;
    private readonly ILogger<EmbeddingService> _logger;

    public EmbeddingService(
        GeminiClient geminiClient,
        IOptions<GeminiOptions> geminiOptions,
        ILogger<EmbeddingService> logger)
    {
        _geminiClient = geminiClient;
        _logger = logger;
        _retryPipeline = RetryPolicies.CreateRetryPipeline(
            geminiOptions.Value.MaxRetries,
            geminiOptions.Value.InitialRetryDelayMs);
    }

    public async Task<float[]> EmbedAsync(string text)
    {
        float[]? result = null;

        await _retryPipeline.ExecuteAsync(async ct =>
        {
            result = await _geminiClient.EmbedAsync(text, ct);
        });

        return result ?? throw new InvalidOperationException("Embedding returned null.");
    }

    public async Task<List<float[]>> EmbedBatchAsync(List<string> texts)
    {
        var results = new List<float[]>(texts.Count);
        foreach (var text in texts)
        {
            var embedding = await EmbedAsync(text);
            results.Add(embedding);
        }
        return results;
    }
}
