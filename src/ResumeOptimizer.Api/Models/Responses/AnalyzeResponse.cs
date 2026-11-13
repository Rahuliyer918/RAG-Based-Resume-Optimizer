using System.Text.Json.Serialization;

namespace ResumeOptimizer.Api.Models.Responses;

public class AnalyzeResponse
{
    [JsonPropertyName("analysisId")]
    public string AnalysisId { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("results")]
    public List<JobAnalysisResult>? Results { get; set; }

    [JsonPropertyName("error")]
    public string? Error { get; set; }

    [JsonPropertyName("processingDurationMs")]
    public long? ProcessingDurationMs { get; set; }
}
