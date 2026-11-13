namespace ResumeOptimizer.Api.Services.Interfaces;

public interface IEmbeddingService
{
    Task<float[]> EmbedAsync(string text);
    Task<List<float[]>> EmbedBatchAsync(List<string> texts);
}
