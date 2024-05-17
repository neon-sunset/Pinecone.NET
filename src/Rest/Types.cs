using System.Text.Json.Serialization;

namespace Pinecone.Rest;

internal sealed record ListIndexesResult
{
    public required IndexDetails[] Indexes { get; init; }
}

internal sealed record CreateIndexRequest
{
    public required string Name { get; init; }
    public required uint Dimension { get; init; }
    public required Metric Metric { get; init; }
    public required IndexSpec Spec { get; init; }
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
