using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using ResumeOptimizer.Api.Configuration;
using ResumeOptimizer.Api.Models.Domain;
using ResumeOptimizer.Api.Models.Domain.Enums;
using ResumeOptimizer.Api.Models.Requests;
using ResumeOptimizer.Api.Models.Responses;
using ResumeOptimizer.Api.Services;
using ResumeOptimizer.Api.Services.Interfaces;

namespace ResumeOptimizer.Tests.Services;

public class AnalysisOrchestratorTests
{
    private readonly Mock<IPdfParserService> _pdfParser = new();
    private readonly Mock<IResumeStructuringService> _structuringService = new();
    private readonly Mock<IChunkingService> _chunkingService = new();
    private readonly Mock<IEmbeddingService> _embeddingService = new();
    private readonly Mock<IVectorSearchService> _vectorSearchService = new();
    private readonly Mock<IGenerationService> _generationService = new();
    private readonly Mock<IAnalysisRepository> _repository = new();
    private readonly AnalysisOrchestrator _sut;

    public AnalysisOrchestratorTests()
    {
        var appOptions = Options.Create(new AppOptions { VectorSearchTopK = 10 });
        _sut = new AnalysisOrchestrator(
            _pdfParser.Object,
            _structuringService.Object,
            _chunkingService.Object,
            _embeddingService.Object,
            _vectorSearchService.Object,
            _generationService.Object,
            _repository.Object,
            appOptions,
            NullLogger<AnalysisOrchestrator>.Instance);
    }

    [Fact]
    public async Task InitiateAsync_NoResumeAndNoText_ReturnsValidationError()
    {
        var request = new AnalyzeRequest { JobDescriptions = ["some JD"] };

        var (analysisId, error) = await _sut.InitiateAsync(request);

        Assert.Empty(analysisId);
        Assert.NotNull(error);
        Assert.Contains("resume", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task InitiateAsync_NoJobDescriptions_ReturnsValidationError()
    {
        _pdfParser.Setup(p => p.ValidateText(It.IsAny<string>()))
            .Returns(new PdfParseResult("valid text content here...", true));

        var request = new AnalyzeRequest
        {
            ResumeText = "valid text content here...",
            JobDescriptions = []
        };

        var (analysisId, error) = await _sut.InitiateAsync(request);

        Assert.Empty(analysisId);
        Assert.NotNull(error);
        Assert.Contains("job description", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task InitiateAsync_InvalidResumeText_ReturnsValidationError()
    {
        _pdfParser.Setup(p => p.ValidateText(It.IsAny<string>()))
            .Returns(new PdfParseResult(string.Empty, false, "Text too short. Please use resumeText field."));

        var request = new AnalyzeRequest
        {
            ResumeText = "short",
            JobDescriptions = ["some JD"]
        };

        var (analysisId, error) = await _sut.InitiateAsync(request);

        Assert.Empty(analysisId);
        Assert.NotNull(error);
    }

    [Fact]
    public async Task InitiateAsync_ValidRequest_SavesDocumentAndReturnsId()
    {
        _pdfParser.Setup(p => p.ValidateText(It.IsAny<string>()))
            .Returns(new PdfParseResult("valid resume text...", true));
        _repository.Setup(r => r.SaveAnalysisAsync(It.IsAny<AnalysisDocument>()))
            .ReturnsAsync((AnalysisDocument d) => d.AnalysisId);

        var request = new AnalyzeRequest
        {
            ResumeText = "valid resume text...",
            JobDescriptions = ["Job Description 1"]
        };

        var (analysisId, error) = await _sut.InitiateAsync(request);

        Assert.Null(error);
        Assert.NotEmpty(analysisId);
        _repository.Verify(r => r.SaveAnalysisAsync(It.Is<AnalysisDocument>(d =>
            d.Status == AnalysisStatus.Processing &&
            d.JobDescriptions.Count == 1)), Times.Once);
    }

    [Fact]
    public async Task RunPipelineAsync_Success_UpdatesStatusToComplete()
    {
        var analysisId = Guid.NewGuid().ToString();
        var sessionId = Guid.NewGuid().ToString();
        var doc = new AnalysisDocument
        {
            AnalysisId = analysisId,
            SessionId = sessionId,
            ResumeTextRaw = "resume text",
            Style = ResumeStyle.MetricsFocused,
            JobDescriptions = ["JD 1"]
        };

        var chunks = new List<ResumeChunk>
        {
            new() { SessionId = sessionId, Text = "Built APIs", Type = ChunkType.ExperienceBullet }
        };

        _repository.Setup(r => r.GetByAnalysisIdAsync(analysisId)).ReturnsAsync(doc);
        _structuringService.Setup(s => s.StructureAsync(It.IsAny<string>()))
            .ReturnsAsync(new StructuredResume(null, [], [], [], []));
        _chunkingService.Setup(c => c.Chunk(It.IsAny<StructuredResume>(), It.IsAny<string>()))
            .Returns(chunks);
        _embeddingService.Setup(e => e.EmbedBatchAsync(It.IsAny<List<string>>()))
            .ReturnsAsync([new float[768]]);
        _embeddingService.Setup(e => e.EmbedAsync(It.IsAny<string>()))
            .ReturnsAsync(new float[768]);
        _vectorSearchService.Setup(v => v.SearchAsync(It.IsAny<string>(), It.IsAny<float[]>(), It.IsAny<int>()))
            .ReturnsAsync(chunks);
        _generationService.Setup(g => g.AnalyzeAsync(It.IsAny<List<ResumeChunk>>(), It.IsAny<string>(),
                It.IsAny<ResumeStyle>(), It.IsAny<int>()))
            .ReturnsAsync(new JobAnalysisResult { JobDescriptionIndex = 0, MatchScore = 75f });

        await _sut.RunPipelineAsync(analysisId);

        _repository.Verify(r => r.UpdateStatusAsync(
            analysisId,
            AnalysisStatus.Complete,
            It.Is<List<JobAnalysisResult>>(results => results.Count == 1),
            null,
            It.IsAny<long?>()), Times.Once);
    }

    [Fact]
    public async Task RunPipelineAsync_ExceptionThrown_UpdatesStatusToFailed()
    {
        var analysisId = Guid.NewGuid().ToString();
        var doc = new AnalysisDocument
        {
            AnalysisId = analysisId,
            SessionId = Guid.NewGuid().ToString(),
            ResumeTextRaw = "resume",
            JobDescriptions = ["JD"]
        };

        _repository.Setup(r => r.GetByAnalysisIdAsync(analysisId)).ReturnsAsync(doc);
        _structuringService.Setup(s => s.StructureAsync(It.IsAny<string>()))
            .ThrowsAsync(new InvalidOperationException("Gemini timeout"));

        await _sut.RunPipelineAsync(analysisId);

        _repository.Verify(r => r.UpdateStatusAsync(
            analysisId,
            AnalysisStatus.Failed,
            null,
            It.Is<string>(e => e.Contains("Gemini timeout")),
            It.IsAny<long?>()), Times.Once);
    }
}
