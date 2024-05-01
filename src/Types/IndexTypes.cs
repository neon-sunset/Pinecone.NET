using System.Text.Json.Serialization;
using Pinecone.Rest;

namespace Pinecone;

public record ListIndexesResult
{
    public required IndexDetails[] Indexes { get; init; }
}

public record IndexDetails
{
    public required string Name { get; init; }
    public required uint Dimension { get; init; }
    public required Metric Metric { get; init; }
    public string? Host { get; init; }

    public required IndexSpec Spec { get; init;}
    public required IndexStatus Status { get; init; }
}

[JsonConverter(typeof(JsonStringEnumConverter<Metric>))]
public enum Metric
{
    [JsonPropertyName("cosine")] Cosine = 0,
    [JsonPropertyName("dotproduct")] DotProduct = 1,
    [JsonPropertyName("euclidean")] Euclidean = 2
}

public record IndexStatus
{
    [JsonPropertyName("ready")]
    public required bool IsReady { get; init; }
    public required IndexState State { get; init; }
}

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

public record IndexSpec
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ServerlessSpec? Serverless { get; init; }
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public PodSpec? Pod { get; init; }
}

public record ServerlessSpec
{
    public required string Cloud { get; init; }
    public required string Region { get; init; }
}

public record PodSpec
{
    [JsonPropertyName("environment")]
    public required string Environment { get; init; }
    public long? Replicas { get; init; }
    [JsonPropertyName("pod_type")]
    public required string PodType { get; init; }
    public long Pods { get; init; }
    [JsonPropertyName("metadata_config")]
    public MetadataMap? MetadataConfig { get; init; }
    [JsonPropertyName("source_collection")]
    public string? SourceCollection { get; init; }
}

public record IndexStats
{
    [JsonConverter(typeof(IndexNamespaceArrayConverter))]
    public required IndexNamespace[] Namespaces { get; init; }
    public required uint Dimension { get; init; }
    public required float IndexFullness { get; init; }
    public required uint TotalVectorCount { get; init; }
}

public readonly record struct IndexNamespace
{
    public required string Name { get; init; }
    public required uint VectorCount { get; init; }
}
