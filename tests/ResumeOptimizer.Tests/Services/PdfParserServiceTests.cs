using Microsoft.Extensions.Options;
using ResumeOptimizer.Api.Configuration;
using ResumeOptimizer.Api.Services;

namespace ResumeOptimizer.Tests.Services;

public class PdfParserServiceTests
{
    private readonly PdfParserService _sut;

    public PdfParserServiceTests()
    {
        var options = Options.Create(new AppOptions { MinResumeCharacters = 200 });
        _sut = new PdfParserService(options);
    }

    [Fact]
    public void ValidateText_ValidResume_ReturnsValid()
    {
        var text = """
            John Doe | john@example.com | linkedin.com/in/johndoe

            EXPERIENCE
            Software Engineer — Acme Corp (2020–2023)
            • Built REST APIs serving 10M requests/day
            • Reduced latency by 40% through caching

            SKILLS
            C#, .NET, MongoDB, Docker, Kubernetes
            """;

        var result = _sut.ValidateText(text);

        Assert.True(result.IsValid);
        Assert.Null(result.ValidationError);
    }

    [Fact]
    public void ValidateText_TooShort_ReturnsInvalid()
    {
        var result = _sut.ValidateText("Too short.");

        Assert.False(result.IsValid);
        Assert.NotNull(result.ValidationError);
        Assert.Contains("resumeText", result.ValidationError);
    }

    [Fact]
    public void ValidateText_LongButNoStructuralMarkers_ReturnsInvalid()
    {
        var text = new string('a', 300); // 300 chars but no dates, email, or section headers

        var result = _sut.ValidateText(text);

        Assert.False(result.IsValid);
        Assert.NotNull(result.ValidationError);
        Assert.Contains("resumeText", result.ValidationError);
    }

    [Fact]
    public void ValidateText_HasEmailMarker_ReturnsValid()
    {
        var text = "john.doe@gmail.com " + new string('x', 200);

        var result = _sut.ValidateText(text);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void ValidateText_HasDateRange_ReturnsValid()
    {
        var text = "Worked from 2019–2022 " + new string('x', 200);

        var result = _sut.ValidateText(text);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void ValidateText_HasSectionHeader_ReturnsValid()
    {
        var text = "EXPERIENCE " + new string('x', 200);

        var result = _sut.ValidateText(text);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void ValidateText_TrimsLeadingAndTrailingWhitespace()
    {
        var text = "  \n  john@test.com " + new string('x', 200) + "  \n  ";

        var result = _sut.ValidateText(text);

        Assert.True(result.IsValid);
        Assert.Equal(text.Trim(), result.Text);
    }

    [Fact]
    public void ValidateText_ExactlyMinLength_WithMarker_ReturnsValid()
    {
        // Exactly 200 chars with email at start
        var text = "a@b.com " + new string('x', 192);
        Assert.Equal(200, text.Length);

        var result = _sut.ValidateText(text);

        Assert.True(result.IsValid);
    }
}
