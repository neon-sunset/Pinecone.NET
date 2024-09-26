using System.Text.Json.Serialization;

namespace Pinecone;

/// <summary>
/// Object storing information about the collection.
/// </summary>
public record CollectionDetails
{
    /// <summary>
    /// The name of the collection.
    /// </summary> 
    public required string Name { get; init; }

    /// <summary>
    /// The size of the collection in bytes.
    /// </summary>
    public long? Size { get; init; }

    /// <summary>
    /// The status of the collection.
    /// </summary> 
    public required CollectionStatus Status { get; init; }

    /// <summary>
    /// The dimension of the vectors stored in each record held in the collection.
    /// </summary>
    public uint? Dimension { get; init; }

    /// <summary>
    /// The number of records stored in the collection.
    /// </summary> 
    [JsonPropertyName("vector_count")]
    public long? VectorCount { get; init; }

    /// <summary>
    /// The environment where the collection is hosted.
    /// </summary>
    public required string Environment { get; init; }
}

/// <summary>
/// Current status of the collection.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<CollectionStatus>))]
public enum CollectionStatus
{
    Initializing = 0,
    Ready = 1,
    Terminating = 2
}
