using ResumeOptimizer.Api.Models.Domain.Enums;
using ResumeOptimizer.Api.Services;
using ResumeOptimizer.Api.Services.Interfaces;

namespace ResumeOptimizer.Tests.Services;

public class ChunkingServiceTests
{
    private readonly ChunkingService _sut = new();
    private const string SessionId = "test-session-123";

    [Fact]
    public void Chunk_EmptyResume_ReturnsEmptyList()
    {
        var structured = new StructuredResume(null, [], [], [], []);
        var result = _sut.Chunk(structured, SessionId);
        Assert.Empty(result);
    }

    [Fact]
    public void Chunk_SummaryOnly_ReturnsSingleSummaryChunk()
    {
        var structured = new StructuredResume("Experienced software engineer.", [], [], [], []);
        var result = _sut.Chunk(structured, SessionId);

        Assert.Single(result);
        Assert.Equal(ChunkType.Summary, result[0].Type);
        Assert.Equal("Experienced software engineer.", result[0].Text);
        Assert.Equal(SessionId, result[0].SessionId);
        Assert.Equal(0, result[0].SequenceIndex);
    }

    [Fact]
    public void Chunk_ExperienceBullets_PreserveRoleAndCompany()
    {
        var exp = new StructuredExperience(
            "Software Engineer",
            "Acme Corp",
            "2021–2023",
            ["Built REST APIs", "Reduced latency by 40%"]);
        var structured = new StructuredResume(null, [exp], [], [], []);

        var result = _sut.Chunk(structured, SessionId);

        Assert.Equal(2, result.Count);
        Assert.All(result, c =>
        {
            Assert.Equal(ChunkType.ExperienceBullet, c.Type);
            Assert.Equal("Software Engineer", c.Role);
            Assert.Equal("Acme Corp", c.Company);
            Assert.Equal("2021–2023", c.DateRange);
        });
        Assert.Equal("Built REST APIs", result[0].Text);
        Assert.Equal("Reduced latency by 40%", result[1].Text);
    }

    [Fact]
    public void Chunk_Skills_EachSkillIsOwnChunk()
    {
        var structured = new StructuredResume(
            null, [],
            ["Python", "Go", "Kubernetes"],
            [], []);

        var result = _sut.Chunk(structured, SessionId);

        Assert.Equal(3, result.Count);
        Assert.All(result, c => Assert.Equal(ChunkType.Skill, c.Type));
        Assert.Equal("Python", result[0].Text);
        Assert.Equal("Go", result[1].Text);
        Assert.Equal("Kubernetes", result[2].Text);
    }

    [Fact]
    public void Chunk_AllSections_SequenceIndexIsContiguous()
    {
        var exp = new StructuredExperience("Dev", "Corp", "2020–2022", ["Bullet 1", "Bullet 2"]);
        var structured = new StructuredResume("Summary text.", [exp], ["C#"], ["BS CS"], ["AWS Cert"]);

        var result = _sut.Chunk(structured, SessionId);

        for (int i = 0; i < result.Count; i++)
            Assert.Equal(i, result[i].SequenceIndex);
    }

    [Fact]
    public void Chunk_EmptyStringsInBullets_AreSkipped()
    {
        var exp = new StructuredExperience("Dev", "Corp", "2020–2022", ["Valid bullet", "", "  "]);
        var structured = new StructuredResume(null, [exp], [], [], []);

        var result = _sut.Chunk(structured, SessionId);

        Assert.Single(result);
        Assert.Equal("Valid bullet", result[0].Text);
    }

    [Fact]
    public void Chunk_MultipleExperiences_AllChunksHaveCorrectContext()
    {
        var exp1 = new StructuredExperience("Senior Dev", "Alpha Inc", "2022–2024", ["Led team of 5"]);
        var exp2 = new StructuredExperience("Junior Dev", "Beta LLC", "2019–2022", ["Shipped features"]);
        var structured = new StructuredResume(null, [exp1, exp2], [], [], []);

        var result = _sut.Chunk(structured, SessionId);

        Assert.Equal(2, result.Count);
        Assert.Equal("Alpha Inc", result[0].Company);
        Assert.Equal("Beta LLC", result[1].Company);
    }

    [Fact]
    public void Chunk_TextIsTrimmed()
    {
        var exp = new StructuredExperience("Dev", "Corp", "2020", ["  Bullet with spaces  "]);
        var structured = new StructuredResume("  Summary  ", [exp], ["  skill  "], [], []);

        var result = _sut.Chunk(structured, SessionId);

        Assert.Equal("Summary", result[0].Text);
        Assert.Equal("Bullet with spaces", result[1].Text);
        Assert.Equal("skill", result[2].Text);
    }

    [Fact]
    public void Chunk_NullRoleAndCompany_StoredAsNull()
    {
        var exp = new StructuredExperience(string.Empty, string.Empty, string.Empty, ["Bullet"]);
        var structured = new StructuredResume(null, [exp], [], [], []);

        var result = _sut.Chunk(structured, SessionId);

        Assert.Null(result[0].Role);
        Assert.Null(result[0].Company);
        Assert.Null(result[0].DateRange);
    }
}
