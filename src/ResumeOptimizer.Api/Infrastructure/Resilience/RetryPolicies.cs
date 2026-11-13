using System.Net;
using Polly;
using Polly.Retry;

namespace ResumeOptimizer.Api.Infrastructure.Resilience;

public static class RetryPolicies
{
    /// <summary>
    /// Retry pipeline for external LLM API calls (Gemini, etc.).
    /// Retries on 429 (rate limit) and 5xx server errors with exponential backoff + jitter.
    /// Does not retry on 4xx client errors (bad request, unauthorized, etc.).
    /// </summary>
    public static ResiliencePipeline CreateRetryPipeline(int maxRetries, int initialDelayMs)
    {
        return new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = maxRetries,
                Delay = TimeSpan.FromMilliseconds(initialDelayMs),
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true,
                ShouldHandle = new PredicateBuilder()
                    .Handle<HttpRequestException>(ex =>
                        ex.StatusCode == HttpStatusCode.TooManyRequests ||
                        ex.StatusCode == HttpStatusCode.ServiceUnavailable ||
                        ex.StatusCode == HttpStatusCode.BadGateway ||
                        ex.StatusCode == HttpStatusCode.InternalServerError)
            })
            .Build();
    }
}
