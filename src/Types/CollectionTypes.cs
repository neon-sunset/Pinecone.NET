using System.Text.Json.Serialization;

namespace Pinecone;

public record CollectionDetails
{
    public required string Name { get; init; }
    public required long Size { get; init; }
    public required string Status { get; init; }
    public required long Dimension { get; init; }
    [JsonPropertyName("vector_count")]
    public required long VectorCount { get; init; }
}
