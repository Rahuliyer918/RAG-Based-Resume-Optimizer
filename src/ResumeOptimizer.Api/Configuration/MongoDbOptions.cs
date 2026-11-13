namespace ResumeOptimizer.Api.Configuration;

public class MongoDbOptions
{
    public const string Section = "MongoDB";

    public string ConnectionString { get; set; } = string.Empty;
    public string DatabaseName { get; set; } = "resume_optimizer";
    public string ChunksCollectionName { get; set; } = "resume_chunks";
    public string AnalysesCollectionName { get; set; } = "analyses";
}
