using ResumeOptimizer.Api.Models.Domain;
using ResumeOptimizer.Api.Models.Domain.Enums;
using ResumeOptimizer.Api.Models.Responses;

namespace ResumeOptimizer.Api.Services.Interfaces;

public interface IGenerationService
{
    Task<JobAnalysisResult> AnalyzeAsync(
        List<ResumeChunk> relevantChunks,
        string jobDescription,
        ResumeStyle style,
        int jobDescriptionIndex);
}
