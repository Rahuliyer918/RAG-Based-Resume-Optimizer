using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Options;
using Polly;
using ResumeOptimizer.Api.Configuration;
using ResumeOptimizer.Api.Infrastructure.Gemini;
using ResumeOptimizer.Api.Infrastructure.Resilience;
using ResumeOptimizer.Api.Models.Domain;
using ResumeOptimizer.Api.Models.Domain.Enums;
using ResumeOptimizer.Api.Models.Responses;
using ResumeOptimizer.Api.Services.Interfaces;

namespace ResumeOptimizer.Api.Services;

public class GenerationService : IGenerationService
{
    private readonly GeminiClient _geminiClient;
    private readonly ResiliencePipeline _retryPipeline;
    private readonly ILogger<GenerationService> _logger;

    private const string SystemPrompt =
        """
        You are a professional resume analyst and career coach. Your job is to compare a
        candidate's resume content against a job description and produce a structured analysis.

        Rules you must follow without exception:
        1. NEVER invent skills, experiences, or achievements not present in the resume chunks.
        2. NEVER suggest adding content the candidate has not demonstrated.
        3. matchScore is based solely on the overlap of demonstrated skills and experience with
           the JD requirements — not on potential or inferred abilities.
        4. underrepresentedSkills must reference actual text from the resume chunks as currentText.
        5. tailoredResume bullets must be rephrasing of existing bullets only — same facts, improved
           articulation and keyword alignment. Never add new facts.
        6. Respond ONLY with a valid JSON object matching the schema provided. No prose, no markdown
           fences, no explanation outside the JSON.
        7. Do NOT list soft skills (e.g. "problem-solving", "attention to detail", "communication",
           "teamwork", "willingness to learn") as trueGaps. These are universal expectations, not
           technical gaps. Only list concrete, learnable technical skills or domain knowledge.
        8. Do NOT list experience duration requirements (e.g. "0–2 years", "3+ years of experience")
           as trueGaps. A candidate who exceeds the stated range is overqualified, not deficient.
           Only list actual missing skills or knowledge areas.
        9. Do NOT list a skill as a trueGap if it is clearly implied by skills the candidate already
           has. Examples: CI/CD pipelines and Azure DevOps imply Git/version control proficiency;
           SQL Server optimization implies relational database knowledge; ASP.NET Core implies
           web application development experience.
        10. matchScore must reflect a weighted distinction between required and preferred qualifications:
            - Required qualifications carry 75% of the total score weight. Each missing required
              skill is a significant penalty (15–25 points depending on how central it is to the
              role's core function).
            - Preferred qualifications carry 25% of the total score weight. Each missing preferred
              skill is a minor penalty (3–8 points).
            - If the candidate is missing one or more skills listed under the JD's REQUIRED section,
              the matchScore must not exceed 70, regardless of how many preferred qualifications
              they satisfy.
            - Skills that define the role's core function (e.g. the primary tool or technology
              named in the job title or first responsibility) carry the heaviest individual weight.
        """;

    private static readonly Dictionary<ResumeStyle, string> StyleInstructions = new()
    {
        [ResumeStyle.MetricsFocused] =
            "Prioritize quantified achievements. Rewrite bullets to lead with numbers, percentages, " +
            "dollar amounts, or scale wherever the original text implies a measurable outcome.",
        [ResumeStyle.Narrative] =
            "Rewrite bullets to tell a coherent story of impact and progression. Use active voice and " +
            "connect actions to outcomes in descriptive language.",
        [ResumeStyle.Concise] =
            "Rewrite bullets to be as brief as possible while preserving all key information. " +
            "Aim for one impactful line per bullet."
    };

    private const string JsonSchema =
        """
        {
          "matchScore": <float 0-100>,
          "alignedSkills": ["<skill>", ...],
          "trueGaps": ["<skill or requirement>", ...],
          "underrepresentedSkills": [
            {
              "skill": "<skill name>",
              "currentText": "<exact or near-exact text from resume chunks>",
              "reason": "<why this is underrepresented for this role>"
            }
          ],
          "tailoredResume": {
            "summary": "<rephrased summary or null>",
            "experienceBullets": [
              {
                "original": "<original bullet text>",
                "tailored": "<rephrased bullet>",
                "role": "<job title>",
                "company": "<company name>"
              }
            ],
            "skillsSection": "<rephrased skills line or null>"
          }
        }
        """;

    public GenerationService(
        GeminiClient geminiClient,
        IOptions<GeminiOptions> geminiOptions,
        ILogger<GenerationService> logger)
    {
        _geminiClient = geminiClient;
        _logger = logger;
        _retryPipeline = RetryPolicies.CreateRetryPipeline(
            geminiOptions.Value.MaxRetries,
            geminiOptions.Value.InitialRetryDelayMs);
    }

    public async Task<JobAnalysisResult> AnalyzeAsync(
        List<ResumeChunk> relevantChunks,
        string jobDescription,
        ResumeStyle style,
        int jobDescriptionIndex)
    {
        var userPrompt = BuildPrompt(relevantChunks, jobDescription, style);
        JobAnalysisResult? finalResult = null;

        await _retryPipeline.ExecuteAsync(async ct =>
        {
            // jsonMode: true — Gemini guarantees valid JSON output, no parsing workarounds needed
            var raw = await _geminiClient.GenerateAsync(userPrompt, SystemPrompt, jsonMode: true, ct);
            finalResult = ParseResult(raw, jobDescriptionIndex);
        });

        if (finalResult is null)
            throw new InvalidOperationException("Generation service returned no valid response.");

        // Sanitize underrepresentedSkills: clear any currentText the model fabricated
        // (i.e. text not actually found in the resume chunks). Log but do not fail.
        var allChunkTexts = relevantChunks.Select(c => c.Text).ToList();
        foreach (var ur in finalResult.UnderrepresentedSkills)
        {
            if (!string.IsNullOrWhiteSpace(ur.CurrentText) &&
                !FuzzyMatchesAnyChunk(ur.CurrentText, allChunkTexts))
            {
                _logger.LogWarning(
                    "underrepresentedSkills.currentText for skill '{Skill}' did not match any chunk — cleared.",
                    ur.Skill);
                ur.CurrentText = string.Empty;
            }
        }

        return finalResult;
    }

    private static string BuildPrompt(List<ResumeChunk> chunks, string jobDescription, ResumeStyle style)
    {
        var sb = new StringBuilder();
        sb.AppendLine("## Resume Chunks (from candidate's resume):");

        foreach (var chunk in chunks)
        {
            var prefix = chunk.Type switch
            {
                ChunkType.Summary => "[SUMMARY]",
                ChunkType.ExperienceBullet =>
                    $"[EXPERIENCE | {chunk.Role ?? "Unknown Role"} at {chunk.Company ?? "Unknown Company"} ({chunk.DateRange ?? ""})]",
                ChunkType.Skill => "[SKILL]",
                ChunkType.Education => "[EDUCATION]",
                ChunkType.Certification => "[CERTIFICATION]",
                _ => "[OTHER]"
            };
            sb.AppendLine($"{prefix} {chunk.Text}");
        }

        sb.AppendLine();
        sb.AppendLine("## Job Description:");
        sb.AppendLine(jobDescription);
        sb.AppendLine();
        sb.AppendLine("## Style Instruction:");
        sb.AppendLine(StyleInstructions[style]);
        sb.AppendLine();
        sb.AppendLine("## Required JSON Response Schema:");
        sb.AppendLine(JsonSchema);

        return sb.ToString();
    }

    private static JobAnalysisResult ParseResult(string json, int jobDescriptionIndex)
    {
        var node = JsonNode.Parse(json)
            ?? throw new JsonException("Generation returned null JSON.");

        var matchScore = node["matchScore"]?.GetValue<float>() ?? 0f;

        var alignedSkills = node["alignedSkills"]?.AsArray()
            .Select(s => s?.GetValue<string>() ?? string.Empty)
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .ToList() ?? [];

        var trueGaps = node["trueGaps"]?.AsArray()
            .Select(s => s?.GetValue<string>() ?? string.Empty)
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .ToList() ?? [];

        var underrepresented = node["underrepresentedSkills"]?.AsArray()
            .Select(u => new UnderrepresentedSkill
            {
                Skill = u?["skill"]?.GetValue<string>() ?? string.Empty,
                CurrentText = u?["currentText"]?.GetValue<string>() ?? string.Empty,
                Reason = u?["reason"]?.GetValue<string>() ?? string.Empty
            })
            .ToList() ?? [];

        var tailoredNode = node["tailoredResume"];
        var tailored = new TailoredResume
        {
            Summary = tailoredNode?["summary"]?.GetValue<string>(),
            SkillsSection = tailoredNode?["skillsSection"]?.GetValue<string>(),
            ExperienceBullets = tailoredNode?["experienceBullets"]?.AsArray()
                .Select(b => new TailoredBullet
                {
                    Original = b?["original"]?.GetValue<string>() ?? string.Empty,
                    Tailored = b?["tailored"]?.GetValue<string>() ?? string.Empty,
                    Role = b?["role"]?.GetValue<string>(),
                    Company = b?["company"]?.GetValue<string>()
                })
                .ToList() ?? []
        };

        return new JobAnalysisResult
        {
            JobDescriptionIndex = jobDescriptionIndex,
            MatchScore = Math.Clamp(matchScore, 0f, 100f),
            AlignedSkills = alignedSkills,
            TrueGaps = trueGaps,
            UnderrepresentedSkills = underrepresented,
            TailoredResume = tailored
        };
    }

    private static bool FuzzyMatchesAnyChunk(string currentText, List<string> chunkTexts)
    {
        var words = currentText.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words.Length < 3) return true;

        for (int i = 0; i <= words.Length - 4; i++)
        {
            var window = string.Join(' ', words[i..(i + 4)]).ToLowerInvariant();
            if (chunkTexts.Any(c => c.Contains(window, StringComparison.OrdinalIgnoreCase)))
                return true;
        }

        return false;
    }
}
