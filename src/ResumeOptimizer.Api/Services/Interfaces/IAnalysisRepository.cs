using ResumeOptimizer.Api.Models.Domain;
using ResumeOptimizer.Api.Models.Responses;

namespace ResumeOptimizer.Api.Services.Interfaces;

public interface IAnalysisRepository
{
    Task<string> SaveAnalysisAsync(AnalysisDocument document);
    Task<AnalysisDocument?> GetByAnalysisIdAsync(string analysisId);
    Task UpdateStatusAsync(string analysisId, string status, List<JobAnalysisResult>? results = null,
        string? error = null, long? durationMs = null);
    Task SaveChunksAsync(List<ResumeChunk> chunks);
}
