using Microsoft.Extensions.Options;
using MongoDB.Driver;
using ResumeOptimizer.Api.Configuration;
using ResumeOptimizer.Api.Models.Domain;

namespace ResumeOptimizer.Api.Infrastructure.MongoDB;

public class MongoDbContext
{
    private readonly IMongoDatabase _database;

    public MongoDbContext(IOptions<MongoDbOptions> options)
    {
        var opts = options.Value;
        var client = new MongoClient(opts.ConnectionString);
        _database = client.GetDatabase(opts.DatabaseName);
        Chunks = _database.GetCollection<ResumeChunk>(opts.ChunksCollectionName);
        Analyses = _database.GetCollection<AnalysisDocument>(opts.AnalysesCollectionName);
    }

    public IMongoCollection<ResumeChunk> Chunks { get; }
    public IMongoCollection<AnalysisDocument> Analyses { get; }
}
