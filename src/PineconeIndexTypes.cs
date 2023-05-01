using System.Text.Json.Serialization;
using Pinecone.Transport;
using Pinecone.Transport.Rest;

namespace Pinecone;

public readonly record struct PineconeIndexName(string Value)
{
    public static implicit operator string(PineconeIndexName value) => value.Value;
    public static implicit operator PineconeIndexName(string value) => new(value);
}

public partial record PineconeIndex<TTransport>
    where TTransport : struct, ITransport<TTransport>
{
    [JsonPropertyName("database")]
    public required PineconeIndexDetails Details { get; init; }

    [JsonPropertyName("status")]
    public required PineconeIndexStatus Status { get; init; }

    [JsonPropertyName("metadata_config")]
    public Dictionary<string, string[]>? MetadataConfig { get; init; }
}

public record PineconeIndexDetails
{
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("dimension")]
    public required long Dimension { get; init; }

    [JsonPropertyName("metric")]
    public required PineconeMetric Metric { get; init; }

    [JsonPropertyName("pods")]
    public long? Pods { get; init; }

    [JsonPropertyName("pod_type")]
    public string? PodType { get; init; }

    [JsonPropertyName("replicas")]
    public long? Replicas { get; init; }
}

[JsonConverter(typeof(PineconeMetricConverter))]
public enum PineconeMetric
{
    Cosine = 0,
    DotProduct = 1,
    Euclidean = 2,
}

public record PineconeIndexStatus
{
    [JsonPropertyName("ready")]
    public required bool IsReady { get; init; }

    [JsonPropertyName("state")]
    public required PineconeIndexState State { get; init; }

    [JsonPropertyName("host")]
    public required string Host { get; init; }

    [JsonPropertyName("waiting")]
    public string?[]? Waiting { get; init; }

    [JsonPropertyName("crashed")]
    public string?[]? Crashed { get; init; }
}

[JsonConverter(typeof(PineconeIndexStateConverter))]
public enum PineconeIndexState
{
    Initializing,
    ScalingUp,
    ScalingDown,
    Terminating,
    Ready
}

public record PineconeIndexStats
{
    public required PineconeIndexNamespace[] Namespaces { get; init; }

    public required uint Dimension { get; init; }

    public required float IndexFullness { get; init; }

    public required uint TotalVectorCount { get; init; }
}

public readonly record struct PineconeIndexNamespace
{
    public required string Name { get; init; }

    public required uint VectorCount { get; init; }
}