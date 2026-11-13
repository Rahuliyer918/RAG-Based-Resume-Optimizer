using ResumeOptimizer.Api.Models.Domain;

namespace ResumeOptimizer.Api.Services.Interfaces;

public interface IChunkingService
{
    List<ResumeChunk> Chunk(StructuredResume structuredResume, string sessionId);
}
