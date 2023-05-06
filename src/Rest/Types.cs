using System.Text.Json.Serialization;

namespace Pinecone.Rest;

internal sealed record CreateIndexRequest : PineconeIndexDetails
{
    [JsonPropertyName("metadata_config")]
    public MetadataMap? MetadataConfig { get; init; }

    [JsonPropertyName("source_collection")]
    public string? SourceCollection { get; init; }

    public static CreateIndexRequest From(
        PineconeIndexDetails index,
        MetadataMap? metadataConfig,
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
    public required int Replicas { get; init; }

    [JsonPropertyName("pod_type")]
    public required string PodType { get; init; }
}

internal readonly record struct DescribeStatsRequest
{
    public MetadataMap? Filter { get; init; }
}

internal record QueryRequest
{
    public string? Id { get; init; }

    public float[]? Vector { get; init; }

    public SparseValues? SparseVector { get; init; }

    public required uint TopK { get; init; }

    public required string Namespace { get; init; }

    public required bool IncludeValues { get; init; }

    public required bool IncludeMetadata { get; init; }
}

internal readonly record struct QueryResponse
{
    public required ScoredVector[] Matches { get; init; }

    public required string Namespace { get; init; }
}

internal readonly record struct UpsertRequest
{
    public required PineconeVector[] Vectors { get; init; }

    public required string Namespace { get; init; }
}

internal readonly record struct UpsertResponse
{
    public required uint UpsertedCount { get; init; }
}

internal record UpdateRequest : PineconeVector
{
    /// <summary>Make sure to not set regular Metadata prop when serializing this</summary>
    public MetadataMap? SetMetadata { get; init; }

    public required string Namespace { get; init; }

    public static UpdateRequest From(
        PineconeVector vector,
        string? indexNamespace) => new()
    {
        Id = vector.Id,
        Values = vector.Values,
        SparseValues = vector.SparseValues,
        SetMetadata = vector.Metadata,
        Namespace = indexNamespace ?? ""
    };
}

internal readonly record struct FetchResponse
{
    public required Dictionary<string, PineconeVector> Vectors { get; init; }

    public required string Namespace { get; init; }
}

internal readonly record struct DeleteRequest
{
    public string[]? Ids { get; init; }

    public required bool DeleteAll { get; init; }

    public MetadataMap? Filter { get; init; }

    public required string Namespace { get; init; }
}
