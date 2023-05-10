using System.Text.Json.Serialization;
using Pinecone.Rest;

namespace Pinecone;

public readonly record struct IndexName(string Value)
{
    public static implicit operator string(IndexName value) => value.Value;
    public static implicit operator IndexName(string value) => new(value);
}

public record IndexDetails
{
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("dimension")]
    public required long Dimension { get; init; }

    [JsonPropertyName("metric")]
    public required Metric Metric { get; init; }

    [JsonPropertyName("pods")]
    public long? Pods { get; init; }

    [JsonPropertyName("pod_type")]
    public string? PodType { get; init; }

    [JsonPropertyName("replicas")]
    public long? Replicas { get; init; }
}

[JsonConverter(typeof(MetricConverter))]
public enum Metric
{
    Cosine = 0,
    DotProduct = 1,
    Euclidean = 2,
}

public record IndexStatus
{
    [JsonPropertyName("ready")]
    public required bool IsReady { get; init; }

    [JsonPropertyName("state")]
    public required IndexState State { get; init; }

    [JsonPropertyName("host")]
    public required string Host { get; init; }

    [JsonPropertyName("waiting")]
    public string?[]? Waiting { get; init; }

    [JsonPropertyName("crashed")]
    public string?[]? Crashed { get; init; }
}

[JsonConverter(typeof(IndexStateConverter))]
public enum IndexState
{
    Initializing = 0,
    ScalingUp = 1,
    ScalingDown = 2,
    Terminating = 3,
    Ready = 4
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
