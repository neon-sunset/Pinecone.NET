namespace Pinecone.Transport;

public interface ITransport<T> : IDisposable
{
    static abstract T Create(string host, string apiKey);

    Task<PineconeIndexStats> DescribeStats(IEnumerable<KeyValuePair<string, MetadataValue>>? filter = null);
    Task<ScoredVector[]> Query(
        string? id,
        float[]? values,
        uint topK,
        string? indexNamespace,
        bool includeValues,
        bool includeMetadata);
    Task<uint> Upsert(IEnumerable<PineconeVector> vectors, string? indexNamespace = null);
    Task Update(PineconeVector vector, string? indexNamespace = null);
    Task<(string Namespace, Dictionary<string, PineconeVector> Vectors)> Fetch(IEnumerable<string> ids, string? indexNamespace = null);
    Task Delete(IEnumerable<string> ids, string? indexNamespace = null);
    Task Delete(IEnumerable<KeyValuePair<string, MetadataValue>> filter, string? indexNamespace = null);
    Task DeleteAll(string? indexNamespace = null);
}
