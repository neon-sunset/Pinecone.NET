using System.Text.Json.Serialization;
using Pinecone.Rest;

namespace Pinecone;

/// <summary>
/// Object storing information about the index.
/// </summary>
public record IndexDetails
{
    /// <summary>
    /// Name of the index.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// The dimension of the indexed vectors.
    /// </summary>
    public required uint Dimension { get; init; }

    /// <summary>
    /// The distance metric used for similarity search.
    /// </summary>
    public required Metric Metric { get; init; }

    /// <summary>
    /// The URL address where the index is hosted.
    /// </summary>
    public required string Host { get; init; }

    /// <summary>
    /// The deletion protection status of the index.
    ///  </summary>
    public DeletionProtection DeletionProtection { get; init; }

    /// <summary>
    /// Additional information about the index.
    /// </summary>
    public required IndexSpec Spec { get; init; }

    /// <summary>
    /// The current status of the index.
    /// </summary>
    public required IndexStatus Status { get; init; }
}

/// <summary>
/// Indicates wherher deletion protection is enabled/disabled for the index.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<DeletionProtection>))]
public enum DeletionProtection
{
    Disabled = 0,
    Enabled = 1
}

/// <summary>
/// The distance metric used for similarity search.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<Metric>))]
public enum Metric
{
    /// <summary>
    /// A measure of the angle between two vectors. It is computed by taking the dot product of the vectors and dividing it by the product of their magnitudes.
    /// </summary>
    [JsonPropertyName("cosine")] Cosine = 0,

    /// <summary>
    /// Calculated by adding the products of the vectors' corresponding components.
    /// </summary>
    [JsonPropertyName("dotproduct")] DotProduct = 1,

    /// <summary>
    /// Straight-line distance between two vectors in a multidimensional space.
    /// </summary>
    [JsonPropertyName("euclidean")] Euclidean = 2
}

/// <summary>
/// Current status of the index.
/// </summary>
public record IndexStatus
{
    /// <summary>
    /// A value indicating whether the index is ready.
    /// </summary>
    [JsonPropertyName("ready")]
    public required bool IsReady { get; init; }

    /// <summary>
    /// Current state of the index.
    /// </summary>
    public required IndexState State { get; init; }
}

/// <summary>
/// Current state of the index.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<IndexState>))]
public enum IndexState
{
    Initializing = 0,
    ScalingUp = 1,
    ScalingDown = 2,
    Terminating = 3,
    Ready = 4,
    ScalingUpPodSize = 5,
    ScalingDownPodSize = 6,
    InitializationFailed = 7
}

/// <summary>
/// Index specification.
/// </summary>
public readonly record struct IndexSpec
{
    /// <summary>
    /// Serverless index specification. <see langword="null" /> if the index is pod-based.
    /// </summary>
    public ServerlessSpec? Serverless { get; init; }

    /// <summary>
    /// Pod-based index specification. <see langword="null" /> if the index is serverless.
    /// </summary>
    public PodSpec? Pod { get; init; }
}

/// <summary>
/// Serverless index specification.
/// </summary>
public readonly record struct ServerlessSpec
{
    /// <summary>
    /// The public cloud where the index is hosted.
    /// </summary>
    public required string Cloud { get; init; }

    /// <summary>
    /// The region where the index has been created.
    /// </summary>
    public required string Region { get; init; }
}

/// <summary>
/// Pod-based index specification.
/// </summary>
public record PodSpec
{
    /// <summary>
    /// The environment where the index is hosted.
    /// </summary>
    public required string Environment { get; init; }

    /// <summary>
    /// The pod type.
    /// </summary>
    [JsonPropertyName("pod_type")]
    public required string PodType { get; init; }

    /// <summary>
    /// The number of pods used.
    /// </summary>
    public required uint Pods { get; init; }

    /// <summary>
    /// The number od replicas.
    /// </summary>
    public required uint Replicas { get; init; }

    /// <summary>
    /// The number of shards.
    /// </summary>
    public required uint Shards { get; init; }

    /// <summary>
    /// Configuration for the behavior of internal metadata index. By default, all metadata is indexed. 
    /// When MetadataConfig is present, only specified metadata fields are indexed.
    /// </summary>
    [JsonPropertyName("metadata_config")]
    public MetadataMap? MetadataConfig { get; init; }

    /// <summary>
    /// The name of the collection used as the source for the index.
    /// </summary>
    [JsonPropertyName("source_collection")]
    public string? SourceCollection { get; init; }
}

/// <summary>
/// Statistics describing the contents of an index.
/// </summary>
public readonly record struct IndexStats
{
    /// <summary>
    /// List of namespaces.
    /// </summary>
    [JsonConverter(typeof(IndexNamespaceArrayConverter))]
    public required IndexNamespace[] Namespaces { get; init; }

    /// <summary>
    /// The dimension of the indexed vectors.
    /// </summary>
    public required uint Dimension { get; init; }

    /// <summary>
    /// The fullness of the index, regardless of whether a metadata filter expression was passed.
    /// </summary>
    public required float IndexFullness { get; init; }

    /// <summary>
    /// Total number of vectors stored in the index.
    /// </summary>
    public required uint TotalVectorCount { get; init; }
}

/// <summary>
/// Information about a single namespace.
/// </summary>
public readonly record struct IndexNamespace
{
    /// <summary>
    /// Namespace name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Number of vectors stored in the namespace.
    /// </summary>
    public required uint VectorCount { get; init; }
}
