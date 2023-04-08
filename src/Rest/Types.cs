using System.Text.Json.Serialization;

namespace Pinecone.Transport.Rest;

internal sealed record CreateIndexRequest : PineconeIndexDetails
{
    [JsonPropertyName("metadata_config")]
    public Dictionary<string, string[]>? MetadataConfig { get; init; }

    [JsonPropertyName("source_collection")]
    public string? SourceCollection { get; init; }

    public static CreateIndexRequest From(
        PineconeIndexDetails index,
        Dictionary<string, string[]>? metadataConfig,
        string? sourceCollection) => new()
    {
        Dimension = index.Dimension,
        Metric = index.Metric,
        Name = index.Name,
        Pods = index.Pods,
        PodType = index.PodType,
        Replicas = index.Replicas,
        MetadataConfig = metadataConfig,
        SourceCollection = sourceCollection
    };
}

internal readonly record struct ConfigureIndexRequest
{
    [JsonPropertyName("replicas")]
    public required int Replicas { get; init; }

    [JsonPropertyName("pod_type")]
    public required string PodType { get; init; }
}
