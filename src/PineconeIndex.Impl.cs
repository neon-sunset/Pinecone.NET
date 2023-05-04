using System.Text.Json.Serialization;

namespace Pinecone;

public sealed partial record PineconeIndex<TTransport> : IDisposable
    where TTransport : struct, ITransport<TTransport>
{
    [JsonIgnore]
    internal TTransport Transport { get; set; }

    public Task<PineconeIndexStats> DescribeStats(MetadataMap? filter = null)
    {
        return Transport.DescribeStats(filter);
    }

    public Task<ScoredVector[]> Query(
        float[] values,
        uint topK,
        string? indexNamespace = null,
        bool includeValues = true,
        bool includeMetadata = false)
    {
        return Transport.Query(null, values, topK, indexNamespace, includeValues, includeMetadata);
    }

    public Task<ScoredVector[]> QueryById(
        string id,
        uint topK,
        string? indexNamespace = null,
        bool includeValues = true,
        bool includeMetadata = false)
    {
        return Transport.Query(id, null, topK, indexNamespace, includeValues, includeMetadata);
    }

    public Task<uint> Upsert(IEnumerable<PineconeVector> vectors, string? indexNamespace = null)
    {
        return Transport.Upsert(vectors, indexNamespace);
    }

    public Task Update(PineconeVector vector, string? indexNamespace = null)
    {
        return Transport.Update(vector, indexNamespace);
    }

    public Task<Dictionary<string, PineconeVector>> Fetch(IEnumerable<string> ids, string? indexNamespace = null)
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
