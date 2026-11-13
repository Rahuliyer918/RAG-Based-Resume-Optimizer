using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using ResumeOptimizer.Api.Models.Domain.Enums;

namespace ResumeOptimizer.Api.Models.Domain;

public class ResumeChunk
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonElement("sessionId")]
    public string SessionId { get; set; } = string.Empty;

    [BsonElement("text")]
    public string Text { get; set; } = string.Empty;

    [BsonElement("type")]
    [BsonRepresentation(BsonType.String)]
    public ChunkType Type { get; set; }

    [BsonElement("role")]
    public string? Role { get; set; }

    [BsonElement("company")]
    public string? Company { get; set; }

    [BsonElement("dateRange")]
    public string? DateRange { get; set; }

    [BsonElement("sequenceIndex")]
    public int SequenceIndex { get; set; }

    [BsonElement("embedding")]
    public float[]? Embedding { get; set; }

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
