using System.Text.Json.Serialization;

namespace ResumeOptimizer.Api.Models.Responses;

public class TailoredResume
{
    [JsonPropertyName("summary")]
    public string? Summary { get; set; }

    [JsonPropertyName("experienceBullets")]
    public List<TailoredBullet> ExperienceBullets { get; set; } = [];

    [JsonPropertyName("skillsSection")]
    public string? SkillsSection { get; set; }
}

public class TailoredBullet
{
    [JsonPropertyName("original")]
    public string Original { get; set; } = string.Empty;

    [JsonPropertyName("tailored")]
    public string Tailored { get; set; } = string.Empty;

    [JsonPropertyName("role")]
    public string? Role { get; set; }

    [JsonPropertyName("company")]
    public string? Company { get; set; }
}
