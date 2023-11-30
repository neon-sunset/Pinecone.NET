using System.Text.Json.Serialization;

namespace Pinecone.Rest;

internal sealed record CreateIndexRequest : IndexDetails
{
    [JsonPropertyName("source_collection")]
    public string? SourceCollection { get; init; }

    public static CreateIndexRequest From(
        IndexDetails index,
        string? sourceCollection) => new()
    {
        Dimension = index.Dimension,
        Metric = index.Metric,
        Name = index.Name,
        Pods = index.Pods,
        PodType = index.PodType,
        Replicas = index.Replicas,
        MetadataConfig = index.MetadataConfig,
        SourceCollection = sourceCollection
    };
}

internal readonly record struct ConfigureIndexRequest
{
    public int? Replicas { get; init; }

    [JsonPropertyName("pod_type")]
    public string? PodType { get; init; }
}

internal readonly record struct DescribeStatsRequest
{
    public MetadataMap? Filter { get; init; }
}

internal readonly record struct CreateCollectionRequest
{
    public required string Name { get; init; }
    public required string Source { get; init; }
}

internal record QueryRequest
{
    public string? Id { get; set; }
    public float[]? Vector { get; set; }
    public SparseVector? SparseVector { get; set; }
    public required uint TopK { get; init; }
    public MetadataMap? Filter { get; init; }
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
    public required IEnumerable<Vector> Vectors { get; init; }
    public required string Namespace { get; init; }
}

internal readonly record struct UpsertResponse
{
    public required uint UpsertedCount { get; init; }
}

internal record UpdateRequest : Vector
{
    /// <summary>Make sure to not set regular Metadata prop when serializing this</summary>
    public MetadataMap? SetMetadata { get; init; }
    public required string Namespace { get; init; }

    public static UpdateRequest From(
        Vector vector,
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
    public required Dictionary<string, Vector> Vectors { get; init; }
    public required string Namespace { get; init; }
}

internal readonly record struct DeleteRequest
{
    public string[]? Ids { get; init; }
    public required bool DeleteAll { get; init; }
    public MetadataMap? Filter { get; init; }
    public required string Namespace { get; init; }
}
