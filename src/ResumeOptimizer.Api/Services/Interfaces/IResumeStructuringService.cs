namespace ResumeOptimizer.Api.Services.Interfaces;

public record StructuredExperience(
    string Role,
    string Company,
    string DateRange,
    List<string> Bullets);

public record StructuredResume(
    string? Summary,
    List<StructuredExperience> Experiences,
    List<string> Skills,
    List<string> Education,
    List<string> Certifications);

public interface IResumeStructuringService
{
    Task<StructuredResume> StructureAsync(string rawResumeText);
}
