namespace Pinecone;

public interface ITransport<T> : IDisposable
{
    static abstract T Create(string host, string apiKey);

    Task<PineconeIndexStats> DescribeStats(MetadataMap? filter = null);
    Task<ScoredVector[]> Query(
        string? id,
        float[]? values,
        uint topK,
        string? indexNamespace,
        bool includeValues,
        bool includeMetadata);
    Task<uint> Upsert(IEnumerable<PineconeVector> vectors, string? indexNamespace = null);
    Task Update(PineconeVector vector, string? indexNamespace = null);
    Task<Dictionary<string, PineconeVector>> Fetch(IEnumerable<string> ids, string? indexNamespace = null);
    Task Delete(IEnumerable<string> ids, string? indexNamespace = null);
    Task Delete(MetadataMap filter, string? indexNamespace = null);
    Task DeleteAll(string? indexNamespace = null);
}
