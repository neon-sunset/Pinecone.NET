namespace Pinecone.Transport;

public interface ITransport<T> : ITransportFactory<T>, IDisposable
{
    Task<PineconeIndexStats> DescribeStats(IEnumerable<KeyValuePair<string, string>>? filter = null);
    Task<ScoredVector[]> Query(
        float[] vector,
        uint topK,
        string?
        indexNamespace = null,
        bool includeValues = false,
        bool includeMetadata = false);
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
