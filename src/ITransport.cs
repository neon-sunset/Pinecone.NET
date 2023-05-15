namespace Pinecone;

public interface ITransport<T> : IDisposable
{
    static abstract T Create(string host, string apiKey);

    Task<IndexStats> DescribeStats(MetadataMap? filter = null);
    Task<ScoredVector[]> Query(
        string? id,
        float[]? values,
        SparseVector? sparseValues,
        uint topK,
        MetadataMap? filter,
        string? indexNamespace,
        bool includeValues,
        bool includeMetadata);
    Task<uint> Upsert(IEnumerable<Vector> vectors, string? indexNamespace = null);
    Task Update(Vector vector, string? indexNamespace = null);
    Task<Dictionary<string, Vector>> Fetch(IEnumerable<string> ids, string? indexNamespace = null);
    Task Delete(IEnumerable<string> ids, string? indexNamespace = null);
    Task Delete(MetadataMap filter, string? indexNamespace = null);
    Task DeleteAll(string? indexNamespace = null);
}
