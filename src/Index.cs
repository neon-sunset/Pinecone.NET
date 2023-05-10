using System.Text.Json.Serialization;

namespace Pinecone;

// Contract
public sealed partial record Index<TTransport>
    where TTransport : struct, ITransport<TTransport>
{
    [JsonPropertyName("database")]
    public required IndexDetails Details { get; init; }

    [JsonPropertyName("status")]
    public required IndexStatus Status { get; init; }

    [JsonPropertyName("metadata_config")]
    public MetadataMap? MetadataConfig { get; init; }
}

// Implementation
public sealed partial record Index<TTransport> : IDisposable
    where TTransport : struct, ITransport<TTransport>
{
    [JsonIgnore]
    internal TTransport Transport { get; set; }

    public Task<IndexStats> DescribeStats(MetadataMap? filter = null)
    {
        return Transport.DescribeStats(filter);
    }

    public Task<ScoredVector[]> Query(
        string id,
        uint topK,
        string? indexNamespace = null,
        bool includeValues = true,
        bool includeMetadata = false)
    {
        return Transport.Query(id, null, null, topK, indexNamespace, includeValues, includeMetadata);
    }

    public Task<ScoredVector[]> Query(
        float[] values,
        uint topK,
        SparseVector? sparseValues = null,
        string? indexNamespace = null,
        bool includeValues = true,
        bool includeMetadata = false)
    {
        return Transport.Query(null, values, sparseValues, topK, indexNamespace, includeValues, includeMetadata);
    }

    public Task<uint> Upsert(IEnumerable<Vector> vectors, string? indexNamespace = null)
    {
        return Transport.Upsert(vectors, indexNamespace);
    }

    public Task Update(Vector vector, string? indexNamespace = null)
    {
        return Transport.Update(vector, indexNamespace);
    }

    public Task<Dictionary<string, Vector>> Fetch(IEnumerable<string> ids, string? indexNamespace = null)
    {
        return Transport.Fetch(ids, indexNamespace);
    }

    public Task Delete(IEnumerable<string> ids, string? indexNamespace = null)
    {
        return Transport.Delete(ids, indexNamespace);
    }

    public Task Delete(MetadataMap filter, string? indexNamespace = null)
    {
        return Transport.Delete(filter, indexNamespace);
    }

    public Task DeleteAll(string? indexNamespace = null)
    {
        return Transport.DeleteAll(indexNamespace);
    }

    public void Dispose() => Transport.Dispose();
}
