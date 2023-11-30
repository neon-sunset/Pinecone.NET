using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace Pinecone;

// Contract
public sealed partial record Index<
#if NET6_0
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
#endif
    TTransport> where TTransport : ITransport<TTransport>
{
    [JsonPropertyName("database")]
    public required IndexDetails Details { get; init; }

    public required IndexStatus Status { get; init; }
}

// Implementation
public sealed partial record Index<TTransport> : IDisposable
    where TTransport : ITransport<TTransport>
{
    [JsonIgnore]
    internal TTransport Transport { get; set; } = default!;

    public Task<IndexStats> DescribeStats(MetadataMap? filter = null)
    {
        return Transport.DescribeStats(filter);
    }

    public Task<ScoredVector[]> Query(
        string id,
        uint topK,
        MetadataMap? filter = null,
        string? indexNamespace = null,
        bool includeValues = true,
        bool includeMetadata = false)
    {
        return Transport.Query(
            id: id,
            values: null,
            sparseValues: null,
            topK: topK,
            filter: filter,
            indexNamespace: indexNamespace,
            includeValues: includeValues,
            includeMetadata: includeMetadata);
    }

    public Task<ScoredVector[]> Query(
        float[] values,
        uint topK,
        MetadataMap? filter = null,
        SparseVector? sparseValues = null,
        string? indexNamespace = null,
        bool includeValues = true,
        bool includeMetadata = false)
    {
        return Transport.Query(
            id: null,
            values: values,
            sparseValues: sparseValues,
            topK: topK,
            filter: filter,
            indexNamespace: indexNamespace,
            includeValues: includeValues,
            includeMetadata: includeMetadata);
    }

    public Task<uint> Upsert(IEnumerable<Vector> vectors, string? indexNamespace = null)
    {
        return Transport.Upsert(vectors, indexNamespace);
    }

    public Task Update(Vector vector, string? indexNamespace = null)
    {
        return Transport.Update(vector, indexNamespace);
    }

    public Task Update(
        string id,
        float[]? values = null,
        SparseVector? sparseValues = null,
        MetadataMap? metadata = null,
        string? indexNamespace = null)
    {
        return Transport.Update(id, values, sparseValues, metadata, indexNamespace);
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
