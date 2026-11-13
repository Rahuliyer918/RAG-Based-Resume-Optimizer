using System.Diagnostics;
using Microsoft.Extensions.Options;
using ResumeOptimizer.Api.Configuration;
using ResumeOptimizer.Api.Models.Domain;
using ResumeOptimizer.Api.Models.Domain.Enums;
using ResumeOptimizer.Api.Models.Requests;
using ResumeOptimizer.Api.Models.Responses;
using ResumeOptimizer.Api.Services.Interfaces;

namespace ResumeOptimizer.Api.Services;

public class AnalysisOrchestrator
{
    private readonly IPdfParserService _pdfParser;
    private readonly IResumeStructuringService _structuringService;
    private readonly IChunkingService _chunkingService;
    private readonly IEmbeddingService _embeddingService;
    private readonly IVectorSearchService _vectorSearchService;
    private readonly IGenerationService _generationService;
    private readonly IAnalysisRepository _repository;
    private readonly AppOptions _appOptions;
    private readonly ILogger<AnalysisOrchestrator> _logger;

    public AnalysisOrchestrator(
        IPdfParserService pdfParser,
        IResumeStructuringService structuringService,
        IChunkingService chunkingService,
        IEmbeddingService embeddingService,
        IVectorSearchService vectorSearchService,
        IGenerationService generationService,
        IAnalysisRepository repository,
        IOptions<AppOptions> appOptions,
        ILogger<AnalysisOrchestrator> logger)
    {
        _pdfParser = pdfParser;
        _structuringService = structuringService;
        _chunkingService = chunkingService;
        _embeddingService = embeddingService;
        _vectorSearchService = vectorSearchService;
        _generationService = generationService;
        _repository = repository;
        _appOptions = appOptions.Value;
        _logger = logger;
    }

    /// <summary>
    /// Validates the request, saves an initial "processing" document, and returns analysisId.
    /// The caller is responsible for firing RunPipelineAsync in the background.
    /// </summary>
    public async Task<(string AnalysisId, string? ValidationError)> InitiateAsync(AnalyzeRequest request)
    {
        // Resolve resume text
        string resumeText;
        if (!string.IsNullOrWhiteSpace(request.ResumeText))
        {
            var textValidation = _pdfParser.ValidateText(request.ResumeText);
            if (!textValidation.IsValid)
                return (string.Empty, textValidation.ValidationError);
            resumeText = textValidation.Text;
        }
        else if (request.Resume is not null)
        {
            var parseResult = await _pdfParser.ParseAsync(request.Resume);
            if (!parseResult.IsValid)
                return (string.Empty, parseResult.ValidationError);
            resumeText = parseResult.Text;
        }
        else
        {
            return (string.Empty,
                "Either 'resume' (PDF file) or 'resumeText' (plain text) must be provided.");
        }

        if (request.JobDescriptions.Count == 0)
            return (string.Empty, "At least one job description must be provided.");

        var style = ParseStyle(request.Style);
        var analysisId = Guid.NewGuid().ToString();
        var sessionId = Guid.NewGuid().ToString();

        var document = new AnalysisDocument
        {
            AnalysisId = analysisId,
            SessionId = sessionId,
            Status = AnalysisStatus.Processing,
            ResumeTextRaw = resumeText,
            Style = style,
            JobDescriptions = request.JobDescriptions
        };

        await _repository.SaveAnalysisAsync(document);
        return (analysisId, null);
    }

    /// <summary>
    /// Runs the full RAG pipeline in the background.
    /// Updates the analysis document status on completion or failure.
    /// </summary>
    public async Task RunPipelineAsync(string analysisId)
    {
        var sw = Stopwatch.StartNew();

        try
        {
            var document = await _repository.GetByAnalysisIdAsync(analysisId)
                ?? throw new InvalidOperationException($"Analysis {analysisId} not found.");

            _logger.LogInformation("Starting pipeline for analysis {AnalysisId}", analysisId);

            // Step 1: Structure resume via Gemini pre-pass
            var structured = await _structuringService.StructureAsync(document.ResumeTextRaw);

            // Step 2: Chunk the structured resume
            var chunks = _chunkingService.Chunk(structured, document.SessionId);
            _logger.LogInformation("Created {ChunkCount} chunks for session {SessionId}",
                chunks.Count, document.SessionId);

            // Step 3: Embed all chunks
            var chunkTexts = chunks.Select(c => c.Text).ToList();
            var embeddings = await _embeddingService.EmbedBatchAsync(chunkTexts);
            for (int i = 0; i < chunks.Count; i++)
                chunks[i].Embedding = embeddings[i];

            // Step 4: Save chunks to MongoDB
            await _repository.SaveChunksAsync(chunks);

            // Step 5: For each JD — embed, search, generate
            var results = new List<Models.Responses.JobAnalysisResult>();
            for (int i = 0; i < document.JobDescriptions.Count; i++)
            {
                var jd = document.JobDescriptions[i];
                _logger.LogInformation("Processing JD {Index}/{Total} for analysis {AnalysisId}",
                    i + 1, document.JobDescriptions.Count, analysisId);

                var jdEmbedding = await _embeddingService.EmbedAsync(jd);
                var relevantChunks = await _vectorSearchService.SearchAsync(
                    document.SessionId, jdEmbedding, _appOptions.VectorSearchTopK);

                // Always include education, skills, and certifications chunks so the
                // model never misses qualifications when scoring gaps.
                var retrievedTexts = relevantChunks.Select(c => c.Text).ToHashSet();
                var pinned = chunks
                    .Where(c => c.Type is ChunkType.Education
                                       or ChunkType.Skill
                                       or ChunkType.Certification
                        && !retrievedTexts.Contains(c.Text))
                    .ToList();
                var allChunks = relevantChunks.Concat(pinned).ToList();

                var result = await _generationService.AnalyzeAsync(
                    allChunks, jd, document.Style, i);
                results.Add(result);
            }

            sw.Stop();
            await _repository.UpdateStatusAsync(
                analysisId, AnalysisStatus.Complete,
                results: results,
                durationMs: sw.ElapsedMilliseconds);

            _logger.LogInformation("Analysis {AnalysisId} completed in {Duration}ms",
                analysisId, sw.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "Pipeline failed for analysis {AnalysisId}", analysisId);
            await _repository.UpdateStatusAsync(
                analysisId, AnalysisStatus.Failed,
                error: ex.Message,
                durationMs: sw.ElapsedMilliseconds);
        }
    }

    private ResumeStyle ParseStyle(string? styleParam)
    {
        return styleParam?.ToLowerInvariant() switch
        {
            "narrative" => ResumeStyle.Narrative,
            "concise" => ResumeStyle.Concise,
            _ => ResumeStyle.MetricsFocused
        };
    }
}
