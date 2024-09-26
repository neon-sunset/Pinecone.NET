using System.Text.Json.Serialization;

namespace Pinecone.Rest;

readonly struct ListIndexesResult
{
    public required IndexDetails[] Indexes { get; init; }
}

sealed class CreateIndexRequest
{
    public required string Name { get; init; }
    public required uint Dimension { get; init; }
    public required Metric Metric { get; init; }
    public required IndexSpec Spec { get; init; }
}

readonly struct ConfigureIndexRequest
{
    public readonly struct ConfigureIndexSpec
    {
        public required PodConfig Pod { get; init; }
    }

    public readonly struct PodConfig
    {
        public int? Replicas { get; init; }
        [JsonPropertyName("pod_type")]
        public string? PodType { get; init; }
    }
    
    public ConfigureIndexSpec? Spec { get; init; }
    public DeletionProtection? DeletionProtection { get; init; }
}

readonly struct DescribeStatsRequest
{
    public MetadataMap? Filter { get; init; }
}

readonly struct CreateCollectionRequest
{
    public required string Name { get; init; }
    public required string Source { get; init; }
}

readonly struct ListCollectionsResult
{
    public required CollectionDetails[] Collections { get; init; }
}

sealed class QueryRequest
{
    public string? Id { get; set; }
    public ReadOnlyMemory<float>? Vector { get; set; }
    public SparseVector? SparseVector { get; set; }
    public required uint TopK { get; init; }
    public MetadataMap? Filter { get; init; }
    public required string Namespace { get; init; }
    public required bool IncludeValues { get; init; }
    public required bool IncludeMetadata { get; init; }
}

readonly struct QueryResponse
{
    public required ScoredVector[] Matches { get; init; }
    public required string Namespace { get; init; }
}

readonly struct UpsertRequest
{
    public required IEnumerable<Vector> Vectors { get; init; }
    public required string Namespace { get; init; }
}

readonly struct UpsertResponse
{
    public required uint UpsertedCount { get; init; }
}

sealed class UpdateRequest
{
    public required string Id { get; init; }
    public ReadOnlyMemory<float>? Values { get; init; }
    public SparseVector? SparseValues { get; init; }
    public MetadataMap? SetMetadata { get; init; }
    public required string Namespace { get; init; }
}

readonly struct ListResponse
{
    public readonly record struct ListVector(string Id);
    public readonly record struct ListPagination(string? Next);
    public readonly record struct ListUsage(uint ReadUnits);

    public required ListVector[] Vectors { get; init; }
    public ListPagination? Pagination { get; init; }
    public required ListUsage Usage { get; init; }
}


readonly struct FetchResponse
{
    public required Dictionary<string, Vector> Vectors { get; init; }
    public required string Namespace { get; init; }
}

readonly struct DeleteRequest
{
    public string[]? Ids { get; init; }
    public required bool DeleteAll { get; init; }
    public MetadataMap? Filter { get; init; }
    public string? Namespace { get; init; }
}
