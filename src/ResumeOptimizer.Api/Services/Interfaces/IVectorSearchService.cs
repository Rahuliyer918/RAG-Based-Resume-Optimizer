using ResumeOptimizer.Api.Models.Domain;

namespace ResumeOptimizer.Api.Services.Interfaces;

public interface IVectorSearchService
{
    Task<List<ResumeChunk>> SearchAsync(string sessionId, float[] queryEmbedding, int topK = 10);
}
