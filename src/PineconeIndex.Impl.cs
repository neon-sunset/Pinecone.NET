using System.Text.Json.Serialization;
using Pinecone.Transport;

namespace Pinecone;

public sealed partial record PineconeIndex<TTransport> : IDisposable
    where TTransport : struct, ITransport<TTransport>
{
    [JsonIgnore]
    internal TTransport Transport { get; set; }

    public Task<PineconeIndexStats> DescribeStats(
        IEnumerable<KeyValuePair<string, MetadataValue>>? filter = null)
    {
        return Transport.DescribeStats(filter);
    }

    public Task<ScoredVector[]> Query(
        float[] values,
        uint topK,
        string? indexNamespace = null,
        bool includeValues = false,
        bool includeMetadata = false)
    {
        return Transport.Query(
            null, values, topK, indexNamespace, includeValues, includeMetadata);
    }

    public Task<ScoredVector[]> QueryById(
        string id,
        uint topK,
        string? indexNamespace = null,
        bool includeValues = false,
        bool includeMetadata = false)
    {
        return Transport.Query(
            id, null, topK, indexNamespace, includeValues, includeMetadata);
    }

    public Task<uint> Upsert(IEnumerable<PineconeVector> vectors, string? indexNamespace = null)
    {
        return Transport.Upsert(vectors, indexNamespace);
    }

    // TODO: Add actual vector
    public Task Update(PineconeVector vector, string? indexNamespace = null)
    {
        return Transport.Update(vector, indexNamespace);
    }

    public Task<(string Namespace, Dictionary<string, PineconeVector> Vectors)> Fetch(
        IEnumerable<string> ids, string? indexNamespace = null)
    {
        return Transport.Fetch(ids, indexNamespace);
    }

    public Task Delete(IEnumerable<string> ids, string? indexNamespace = null)
    {
        throw new NotImplementedException();
    }

    public Task Delete(IDictionary<string, string> filter, string? indexNamespace = null)
    {
        throw new NotImplementedException();
    }

    public Task DeleteAll(string? indexNamespace = null)
    {
        throw new NotImplementedException();
    }

    public void Dispose() => Transport.Dispose();
}
