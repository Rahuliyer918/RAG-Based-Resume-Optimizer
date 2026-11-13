using ResumeOptimizer.Api.Models.Domain;
using ResumeOptimizer.Api.Models.Domain.Enums;
using ResumeOptimizer.Api.Services.Interfaces;

namespace ResumeOptimizer.Api.Services;

public class ChunkingService : IChunkingService
{
    public List<ResumeChunk> Chunk(StructuredResume structuredResume, string sessionId)
    {
        var chunks = new List<ResumeChunk>();
        int index = 0;

        if (!string.IsNullOrWhiteSpace(structuredResume.Summary))
        {
            chunks.Add(new ResumeChunk
            {
                SessionId = sessionId,
                Text = structuredResume.Summary.Trim(),
                Type = ChunkType.Summary,
                SequenceIndex = index++
            });
        }

        foreach (var exp in structuredResume.Experiences)
        {
            foreach (var bullet in exp.Bullets)
            {
                if (string.IsNullOrWhiteSpace(bullet)) continue;
                chunks.Add(new ResumeChunk
                {
                    SessionId = sessionId,
                    Text = bullet.Trim(),
                    Type = ChunkType.ExperienceBullet,
                    Role = string.IsNullOrWhiteSpace(exp.Role) ? null : exp.Role.Trim(),
                    Company = string.IsNullOrWhiteSpace(exp.Company) ? null : exp.Company.Trim(),
                    DateRange = string.IsNullOrWhiteSpace(exp.DateRange) ? null : exp.DateRange.Trim(),
                    SequenceIndex = index++
                });
            }
        }

        foreach (var skill in structuredResume.Skills)
        {
            if (string.IsNullOrWhiteSpace(skill)) continue;
            chunks.Add(new ResumeChunk
            {
                SessionId = sessionId,
                Text = skill.Trim(),
                Type = ChunkType.Skill,
                SequenceIndex = index++
            });
        }

        foreach (var edu in structuredResume.Education)
        {
            if (string.IsNullOrWhiteSpace(edu)) continue;
            chunks.Add(new ResumeChunk
            {
                SessionId = sessionId,
                Text = edu.Trim(),
                Type = ChunkType.Education,
                SequenceIndex = index++
            });
        }

        foreach (var cert in structuredResume.Certifications)
        {
            if (string.IsNullOrWhiteSpace(cert)) continue;
            chunks.Add(new ResumeChunk
            {
                SessionId = sessionId,
                Text = cert.Trim(),
                Type = ChunkType.Certification,
                SequenceIndex = index++
            });
        }

        return chunks;
    }
}
