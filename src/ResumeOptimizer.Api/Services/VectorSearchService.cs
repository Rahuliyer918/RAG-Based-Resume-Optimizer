using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using ResumeOptimizer.Api.Configuration;
using ResumeOptimizer.Api.Infrastructure.MongoDB;
using ResumeOptimizer.Api.Models.Domain;
using ResumeOptimizer.Api.Services.Interfaces;

namespace ResumeOptimizer.Api.Services;

public class VectorSearchService : IVectorSearchService
{
    private readonly MongoDbContext _context;
    private readonly AppOptions _appOptions;

    private const string IndexName = "resume_chunks_vector_index";

    public VectorSearchService(MongoDbContext context, IOptions<AppOptions> appOptions)
    {
        _context = context;
        _appOptions = appOptions.Value;
    }

    public async Task<List<ResumeChunk>> SearchAsync(string sessionId, float[] queryEmbedding, int topK = 10)
    {
        var queryVector = new BsonArray(queryEmbedding.Select(f => (BsonValue)(double)f));

        var pipeline = new[]
        {
            new BsonDocument("$vectorSearch", new BsonDocument
            {
                { "index", IndexName },
                { "path", "embedding" },
                { "queryVector", queryVector },
                { "numCandidates", topK * 10 },
                { "limit", topK },
                { "filter", new BsonDocument("sessionId", sessionId) }
            }),
            new BsonDocument("$project", new BsonDocument
            {
                { "text", 1 },
                { "type", 1 },
                { "role", 1 },
                { "company", 1 },
                { "dateRange", 1 },
                { "sequenceIndex", 1 },
                { "sessionId", 1 }
            })
        };

        var result = await _context.Chunks
            .Aggregate<ResumeChunk>(pipeline)
            .ToListAsync();

        return result;
    }
}
