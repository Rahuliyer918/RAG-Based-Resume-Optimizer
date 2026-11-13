namespace ResumeOptimizer.Api.Configuration;

public class GeminiOptions
{
    public const string Section = "Gemini";

    public string ApiKey { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://generativelanguage.googleapis.com";
    public string EmbeddingModel { get; set; } = "gemini-embedding-001";
    public string GenerationModel { get; set; } = "gemini-2.5-flash";
    public int MaxRetries { get; set; } = 3;
    public int InitialRetryDelayMs { get; set; } = 2000;
}
