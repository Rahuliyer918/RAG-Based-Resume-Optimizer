using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using Polly;
using ResumeOptimizer.Api.Configuration;
using ResumeOptimizer.Api.Infrastructure.Gemini;
using ResumeOptimizer.Api.Infrastructure.Resilience;
using ResumeOptimizer.Api.Services.Interfaces;

namespace ResumeOptimizer.Api.Services;

public class ResumeStructuringService : IResumeStructuringService
{
    private readonly GeminiClient _geminiClient;
    private readonly ResiliencePipeline _retryPipeline;
    private readonly ILogger<ResumeStructuringService> _logger;

    // DTOs used only for deserialization — typed deserialization silently uses the
    // last value when the model emits duplicate JSON property names (e.g. "role": …, "role": …),
    // unlike JsonNode/JsonObject which throws ArgumentException on duplicate keys.
    private sealed record ExperienceDto(
        [property: JsonPropertyName("role")]      string?       Role,
        [property: JsonPropertyName("company")]   string?       Company,
        [property: JsonPropertyName("dateRange")] string?       DateRange,
        [property: JsonPropertyName("bullets")]   List<string>? Bullets);

    private sealed record StructuredResumeDto(
        [property: JsonPropertyName("summary")]        string?             Summary,
        [property: JsonPropertyName("experiences")]    List<ExperienceDto>? Experiences,
        [property: JsonPropertyName("skills")]         List<string>?        Skills,
        [property: JsonPropertyName("education")]      List<string>?        Education,
        [property: JsonPropertyName("certifications")] List<string>?        Certifications);

    private static readonly JsonSerializerOptions DeserializeOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private const string SystemPrompt =
        "You are a resume parser. Extract and structure the resume content into the exact JSON schema provided. " +
        "Preserve all text verbatim — do not rephrase, summarize, or add anything. " +
        "Respond ONLY with valid JSON. No markdown fences, no prose.";

    private const string UserPromptTemplate =
        """
        Parse the following resume text into this exact JSON structure:

        {
          "summary": "<summary text or null>",
          "experiences": [
            {
              "role": "<job title>",
              "company": "<company name>",
              "dateRange": "<date range string>",
              "bullets": ["<bullet text>", ...]
            }
          ],
          "skills": ["<skill or skill group text>", ...],
          "education": ["<education entry text>", ...],
          "certifications": ["<certification text>", ...]
        }

        Rules:
        - If a section is absent, use null or empty array.
        - Each bullet is one logical unit (one line or one sentence).
        - Extract ALL experience entries with their associated bullets.
        - Skills may be individual items or grouped strings — preserve as-is.
        - Each JSON property name must appear exactly once per object — no duplicate keys.

        Resume text:
        {resumeText}
        """;

    public ResumeStructuringService(
        GeminiClient geminiClient,
        IOptions<GeminiOptions> geminiOptions,
        ILogger<ResumeStructuringService> logger)
    {
        _geminiClient = geminiClient;
        _logger = logger;
        _retryPipeline = RetryPolicies.CreateRetryPipeline(
            geminiOptions.Value.MaxRetries,
            geminiOptions.Value.InitialRetryDelayMs);
    }

    public async Task<StructuredResume> StructureAsync(string rawResumeText)
    {
        var userPrompt = UserPromptTemplate.Replace("{resumeText}", rawResumeText);
        string? jsonResponse = null;

        await _retryPipeline.ExecuteAsync(async ct =>
        {
            // jsonMode: true — Gemini guarantees valid JSON output, no markdown fences or prose preamble
            jsonResponse = await _geminiClient.GenerateAsync(userPrompt, SystemPrompt, jsonMode: true, ct);
        });

        if (string.IsNullOrWhiteSpace(jsonResponse))
            throw new InvalidOperationException("Gemini returned an empty response during resume structuring.");

        return ParseStructuredResume(jsonResponse);
    }

    private StructuredResume ParseStructuredResume(string json)
    {
        // Deserialize to typed DTOs rather than JsonNode so that duplicate property names
        // (an occasional Gemini quirk) are handled gracefully — last value wins instead of throwing.
        StructuredResumeDto? dto;
        try
        {
            dto = JsonSerializer.Deserialize<StructuredResumeDto>(json, DeserializeOptions);
        }
        catch (JsonException ex)
        {
            throw new JsonException($"Resume structuring returned invalid JSON: {ex.Message}", ex);
        }

        if (dto is null)
            throw new JsonException("Resume structuring returned null JSON.");

        var experiences = (dto.Experiences ?? [])
            .Select(e => new StructuredExperience(
                Role:      e.Role      ?? string.Empty,
                Company:   e.Company   ?? string.Empty,
                DateRange: e.DateRange ?? string.Empty,
                Bullets:   (e.Bullets ?? [])
                               .Where(b => !string.IsNullOrWhiteSpace(b))
                               .ToList()))
            .ToList();

        var skills = (dto.Skills ?? [])
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .ToList();

        var education = (dto.Education ?? [])
            .Where(e => !string.IsNullOrWhiteSpace(e))
            .ToList();

        var certifications = (dto.Certifications ?? [])
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .ToList();

        _logger.LogInformation(
            "Parsed resume: {ExperienceCount} experiences, {SkillCount} skills, {EducationCount} education entries.",
            experiences.Count, skills.Count, education.Count);

        return new StructuredResume(dto.Summary, experiences, skills, education, certifications);
    }
}
