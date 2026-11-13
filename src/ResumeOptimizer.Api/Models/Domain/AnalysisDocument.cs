using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using ResumeOptimizer.Api.Models.Domain.Enums;
using ResumeOptimizer.Api.Models.Responses;

namespace ResumeOptimizer.Api.Models.Domain;

public class AnalysisDocument
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonElement("analysisId")]
    public string AnalysisId { get; set; } = string.Empty;

    [BsonElement("sessionId")]
    public string SessionId { get; set; } = string.Empty;

    [BsonElement("status")]
    public string Status { get; set; } = AnalysisStatus.Processing;

    [BsonElement("processingError")]
    public string? ProcessingError { get; set; }

    [BsonElement("resumeTextRaw")]
    public string ResumeTextRaw { get; set; } = string.Empty;

    [BsonElement("style")]
    [BsonRepresentation(BsonType.String)]
    public ResumeStyle Style { get; set; } = ResumeStyle.MetricsFocused;

    [BsonElement("jobDescriptions")]
    public List<string> JobDescriptions { get; set; } = [];

    [BsonElement("results")]
    public List<JobAnalysisResult> Results { get; set; } = [];

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("completedAt")]
    public DateTime? CompletedAt { get; set; }

    [BsonElement("processingDurationMs")]
    public long? ProcessingDurationMs { get; set; }
}

public static class AnalysisStatus
{
    public const string Processing = "processing";
    public const string Complete = "complete";
    public const string Failed = "failed";
}
