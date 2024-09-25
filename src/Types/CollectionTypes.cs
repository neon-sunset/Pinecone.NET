using System.Text.Json.Serialization;

namespace Pinecone;

public record CollectionDetails
{
    public required string Name { get; init; }
    public long? Size { get; init; }
    public required CollectionStatus Status { get; init; }
    public required uint Dimension { get; init; }
    [JsonPropertyName("vector_count")]
    public required long VectorCount { get; init; }
    public required string Environment { get; init; }
}

[JsonConverter(typeof(JsonStringEnumConverter<CollectionStatus>))]
public enum CollectionStatus
{
    Initializing = 0,
    Ready = 1,
    Terminating = 2
}
