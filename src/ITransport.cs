namespace Pinecone.Transport;

public interface ITransport<T> : ITransportFactory<T>, IDisposable
{
    Task<PineconeIndexStats> DescribeStats(IEnumerable<KeyValuePair<string, MetadataValue>>? filter = null);
    Task<ScoredVector[]> Query(
        string? id,
        float[]? values,
        uint topK,
        string? indexNamespace,
        bool includeValues,
        bool includeMetadata);
    Task Delete(IEnumerable<string> ids, string? indexNamespace = null);
    Task Delete(IDictionary<string, string> filter, string? indexNamespace = null);
    Task DeleteAll(string? indexNamespace = null);
    Task Fetch(IEnumerable<string> ids);
    Task Update(object vector, string? indexNamespace = null);
    Task Upsert(ReadOnlyMemory<object> vectors, string? indexNamespace = null);
}

public interface ITransportFactory<T>
{
    abstract static T Create(string host, string apiKey);
}
