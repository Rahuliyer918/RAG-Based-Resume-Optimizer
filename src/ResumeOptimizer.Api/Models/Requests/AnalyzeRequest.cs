using Microsoft.AspNetCore.Mvc;

namespace ResumeOptimizer.Api.Models.Requests;

public class AnalyzeRequest
{
    [FromForm(Name = "resume")]
    public IFormFile? Resume { get; set; }

    [FromForm(Name = "resumeText")]
    public string? ResumeText { get; set; }

    [FromForm(Name = "jobDescriptions")]
    public List<string> JobDescriptions { get; set; } = [];

    [FromForm(Name = "style")]
    public string? Style { get; set; }
}
