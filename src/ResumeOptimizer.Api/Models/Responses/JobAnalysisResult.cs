using System.Text.Json.Serialization;
using MongoDB.Bson.Serialization.Attributes;

namespace ResumeOptimizer.Api.Models.Responses;

public class JobAnalysisResult
{
    [BsonElement("jobDescriptionIndex")]
    [JsonPropertyName("jobDescriptionIndex")]
    public int JobDescriptionIndex { get; set; }

    [BsonElement("matchScore")]
    [JsonPropertyName("matchScore")]
    public float MatchScore { get; set; }

    [BsonElement("alignedSkills")]
    [JsonPropertyName("alignedSkills")]
    public List<string> AlignedSkills { get; set; } = [];

    [BsonElement("trueGaps")]
    [JsonPropertyName("trueGaps")]
    public List<string> TrueGaps { get; set; } = [];

    [BsonElement("underrepresentedSkills")]
    [JsonPropertyName("underrepresentedSkills")]
    public List<UnderrepresentedSkill> UnderrepresentedSkills { get; set; } = [];

    [BsonElement("tailoredResume")]
    [JsonPropertyName("tailoredResume")]
    public TailoredResume TailoredResume { get; set; } = new();
}

public class UnderrepresentedSkill
{
    [BsonElement("skill")]
    [JsonPropertyName("skill")]
    public string Skill { get; set; } = string.Empty;

    [BsonElement("currentText")]
    [JsonPropertyName("currentText")]
    public string CurrentText { get; set; } = string.Empty;

    [BsonElement("reason")]
    [JsonPropertyName("reason")]
    public string Reason { get; set; } = string.Empty;
}
