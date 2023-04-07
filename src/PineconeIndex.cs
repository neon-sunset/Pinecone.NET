using System.Text.Json.Serialization;
using Pinecone.Serialization;

namespace Pinecone;

public readonly record struct PineconeIndexName(string Value)
{
    public static implicit operator string(PineconeIndexName value) => value.Value;
    public static implicit operator PineconeIndexName(string value) => new(value);
}

public partial record PineconeIndex
{
    [JsonPropertyName("database")]
    public required PineconeIndexDetails Details { get; init; }

    [JsonPropertyName("status")]
    public required PineconeIndexStatus Status { get; init; }

    [JsonPropertyName("metadata_config")]
    public Dictionary<string, string[]>? MetadataConfig { get; init; }

    [JsonIgnore]
    internal PineconeClient? Client { get; set; }
}

public record PineconeIndexDetails
{
    [JsonPropertyName("dimension")]
    public required long Dimension { get; init; }

    [JsonPropertyName("metric")]
    public required PineconeMetric Metric { get; init; }

    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("pods")]
    public required long Pods { get; init; }

    [JsonPropertyName("pod_type")]
    public required string PodType { get; init; }

    [JsonPropertyName("replicas")]
    public required long Replicas { get; init; }
}

[JsonConverter(typeof(PineconeMetricConverter))]
public enum PineconeMetric
{
    Cosine,
    DotProduct,
    Euclidean,
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

[JsonConverter(typeof(JsonStringEnumConverter))]
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
    // TODO: check actual response
    /// <summary>
    /// name: vectorCount
    /// </summary>
    // public required Dictionary<string, long> Namespaces { get; init; }

    public required long Dimension { get; init; }

    public required float IndexFullness { get; init; }

    public required long TotalVectorCount { get; init; }
}

public readonly record struct PineconeIndexNamespace
{
    public required string Name { get; init; }
}