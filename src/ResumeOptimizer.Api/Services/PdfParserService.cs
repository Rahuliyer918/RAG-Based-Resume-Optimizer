using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using ResumeOptimizer.Api.Configuration;
using ResumeOptimizer.Api.Services.Interfaces;
using UglyToad.PdfPig;

namespace ResumeOptimizer.Api.Services;

public class PdfParserService : IPdfParserService
{
    private static readonly Regex DatePattern = new(
        @"\b(19|20)\d{2}\s*[-–—]\s*(19|20)\d{2}|\b(Jan|Feb|Mar|Apr|May|Jun|Jul|Aug|Sep|Oct|Nov|Dec)[a-z]*\.?\s*(19|20)\d{2}|\bPresent\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex EmailPattern = new(
        @"\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}\b",
        RegexOptions.Compiled);

    private static readonly Regex SectionHeaderPattern = new(
        @"\b(experience|work history|employment|education|skills|summary|objective|certifications|projects|achievements)\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private readonly AppOptions _appOptions;

    public PdfParserService(IOptions<AppOptions> appOptions)
    {
        _appOptions = appOptions.Value;
    }

    public async Task<PdfParseResult> ParseAsync(IFormFile file)
    {
        try
        {
            await using var stream = file.OpenReadStream();
            using var ms = new MemoryStream();
            await stream.CopyToAsync(ms);
            ms.Position = 0;

            var text = ExtractTextFromPdf(ms.ToArray());
            return ValidateText(text);
        }
        catch (Exception ex)
        {
            return new PdfParseResult(string.Empty, false,
                $"PDF parsing failed: {ex.Message}. Please use the 'resumeText' field to paste your resume as plain text.");
        }
    }

    public PdfParseResult ValidateText(string text)
    {
        var trimmed = text.Trim();

        if (trimmed.Length < _appOptions.MinResumeCharacters)
            return new PdfParseResult(trimmed, false,
                $"Extracted text is too short ({trimmed.Length} characters). " +
                "The PDF may use a non-standard layout. Please use the 'resumeText' field to paste your resume as plain text.");

        var hasStructuralMarkers =
            DatePattern.IsMatch(trimmed) ||
            EmailPattern.IsMatch(trimmed) ||
            SectionHeaderPattern.IsMatch(trimmed);

        if (!hasStructuralMarkers)
            return new PdfParseResult(trimmed, false,
                "The extracted text does not appear to be a resume (no dates, email, or section headers found). " +
                "Please use the 'resumeText' field to paste your resume as plain text.");

        return new PdfParseResult(trimmed, true);
    }

    private static string ExtractTextFromPdf(byte[] pdfBytes)
    {
        var sb = new StringBuilder();
        using var document = PdfDocument.Open(pdfBytes);

        foreach (var page in document.GetPages())
        {
            foreach (var word in page.GetWords())
            {
                sb.Append(word.Text);
                sb.Append(' ');
            }
            sb.AppendLine();
        }

        return sb.ToString();
    }
}
