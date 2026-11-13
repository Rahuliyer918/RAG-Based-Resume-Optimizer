namespace ResumeOptimizer.Api.Configuration;

public class AppOptions
{
    public const string Section = "App";

    public int MinResumeCharacters { get; set; } = 200;
    public string DefaultStyle { get; set; } = "metrics-focused";
    public int VectorSearchTopK { get; set; } = 12;
    public int ChunkTtlDays { get; set; } = 30;
}
