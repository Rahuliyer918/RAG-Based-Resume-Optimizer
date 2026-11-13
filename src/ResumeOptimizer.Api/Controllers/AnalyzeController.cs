using Microsoft.AspNetCore.Mvc;
using ResumeOptimizer.Api.Models.Domain;
using ResumeOptimizer.Api.Models.Requests;
using ResumeOptimizer.Api.Models.Responses;
using ResumeOptimizer.Api.Services;
using ResumeOptimizer.Api.Services.Interfaces;

namespace ResumeOptimizer.Api.Controllers;

[ApiController]
[Route("api/analyze")]
public class AnalyzeController : ControllerBase
{
    private readonly AnalysisOrchestrator _orchestrator;
    private readonly IAnalysisRepository _repository;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AnalyzeController> _logger;

    public AnalyzeController(
        AnalysisOrchestrator orchestrator,
        IAnalysisRepository repository,
        IServiceScopeFactory scopeFactory,
        ILogger<AnalyzeController> logger)
    {
        _orchestrator = orchestrator;
        _repository = repository;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    /// <summary>
    /// Submit a resume and one or more job descriptions for analysis.
    /// Returns 202 Accepted immediately. Poll GET /api/analyze/{id} for results.
    /// </summary>
    [HttpPost]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(AnalyzeResponse), StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SubmitAnalysis([FromForm] AnalyzeRequest request)
    {
        var (analysisId, validationError) = await _orchestrator.InitiateAsync(request);

        if (validationError is not null)
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid request",
                Detail = validationError,
                Status = StatusCodes.Status400BadRequest
            });

        // Fire pipeline in background — do not await
        // Use IServiceScopeFactory (not HttpContext) so the scope stays alive after response is sent
        var scopeFactory = _scopeFactory;
        _ = Task.Run(async () =>
        {
            using var scope = scopeFactory.CreateScope();
            var orchestrator = scope.ServiceProvider.GetRequiredService<AnalysisOrchestrator>();
            await orchestrator.RunPipelineAsync(analysisId);
        });

        return Accepted(new AnalyzeResponse
        {
            AnalysisId = analysisId,
            Status = AnalysisStatus.Processing
        });
    }

    /// <summary>
    /// Poll for analysis results. Status will be "processing", "complete", or "failed".
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(AnalyzeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAnalysis(string id)
    {
        var doc = await _repository.GetByAnalysisIdAsync(id);
        if (doc is null)
            return NotFound(new ProblemDetails
            {
                Title = "Not found",
                Detail = $"No analysis found with id '{id}'.",
                Status = StatusCodes.Status404NotFound
            });

        return Ok(new AnalyzeResponse
        {
            AnalysisId = doc.AnalysisId,
            Status = doc.Status,
            Results = doc.Status == AnalysisStatus.Complete ? doc.Results : null,
            Error = doc.ProcessingError,
            ProcessingDurationMs = doc.ProcessingDurationMs
        });
    }

    /// <summary>
    /// Retrieve the tailored resume variant for a specific job description by its index.
    /// Only available once status is "complete".
    /// </summary>
    [HttpGet("{id}/resume/{index}")]
    [ProducesResponseType(typeof(TailoredResume), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> GetTailoredResume(string id, int index)
    {
        var doc = await _repository.GetByAnalysisIdAsync(id);
        if (doc is null)
            return NotFound(new ProblemDetails
            {
                Title = "Not found",
                Detail = $"No analysis found with id '{id}'.",
                Status = StatusCodes.Status404NotFound
            });

        if (doc.Status != AnalysisStatus.Complete)
            return Conflict(new ProblemDetails
            {
                Title = "Analysis not complete",
                Detail = $"Analysis is currently '{doc.Status}'. Tailored resumes are only available when status is 'complete'.",
                Status = StatusCodes.Status409Conflict
            });

        if (index < 0 || index >= doc.Results.Count)
            return NotFound(new ProblemDetails
            {
                Title = "Index out of range",
                Detail = $"Index {index} is out of range. This analysis has {doc.Results.Count} result(s) (indices 0–{doc.Results.Count - 1}).",
                Status = StatusCodes.Status404NotFound
            });

        return Ok(doc.Results[index].TailoredResume);
    }
}
