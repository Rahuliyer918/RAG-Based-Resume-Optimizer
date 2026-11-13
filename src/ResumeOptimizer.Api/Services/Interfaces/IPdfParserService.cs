namespace ResumeOptimizer.Api.Services.Interfaces;

public record PdfParseResult(string Text, bool IsValid, string? ValidationError = null);

public interface IPdfParserService
{
    Task<PdfParseResult> ParseAsync(IFormFile file);
    PdfParseResult ValidateText(string text);
}
