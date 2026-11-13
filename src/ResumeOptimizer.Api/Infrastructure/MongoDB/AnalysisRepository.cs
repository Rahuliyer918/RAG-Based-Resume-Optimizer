using MongoDB.Bson;
using MongoDB.Driver;
using ResumeOptimizer.Api.Models.Domain;
using ResumeOptimizer.Api.Models.Responses;
using ResumeOptimizer.Api.Services.Interfaces;

namespace ResumeOptimizer.Api.Infrastructure.MongoDB;

public class AnalysisRepository : IAnalysisRepository
{
    private readonly MongoDbContext _context;

    public AnalysisRepository(MongoDbContext context)
    {
        _context = context;
    }

    public async Task<string> SaveAnalysisAsync(AnalysisDocument document)
    {
        await _context.Analyses.InsertOneAsync(document);
        return document.AnalysisId;
    }

    public async Task<AnalysisDocument?> GetByAnalysisIdAsync(string analysisId)
    {
        return await _context.Analyses
            .Find(a => a.AnalysisId == analysisId)
            .FirstOrDefaultAsync();
    }

    public async Task UpdateStatusAsync(string analysisId, string status,
        List<JobAnalysisResult>? results = null, string? error = null, long? durationMs = null)
    {
        var update = Builders<AnalysisDocument>.Update
            .Set(a => a.Status, status)
            .Set(a => a.CompletedAt, DateTime.UtcNow);

        if (results is not null)
            update = update.Set(a => a.Results, results);
        if (error is not null)
            update = update.Set(a => a.ProcessingError, error);
        if (durationMs.HasValue)
            update = update.Set(a => a.ProcessingDurationMs, durationMs);

        await _context.Analyses.UpdateOneAsync(a => a.AnalysisId == analysisId, update);
    }

    public async Task SaveChunksAsync(List<ResumeChunk> chunks)
    {
        if (chunks.Count == 0) return;
        await _context.Chunks.InsertManyAsync(chunks);
    }
}
