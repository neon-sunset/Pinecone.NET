namespace Pinecone.Transport;

public interface ITransport<T> : ITransportFactory<T>, IDisposable
{
    Task Delete(IEnumerable<string> ids, string? indexNamespace = null);
    Task Delete(IDictionary<string, string> filter, string? indexNamespace = null);
    Task DeleteAll(string? indexNamespace = null);
    Task<PineconeIndexStats> DescribeStats(IEnumerable<KeyValuePair<string, string>>? filter = null);
    Task Fetch(IEnumerable<string> ids);
    Task Query(ReadOnlyMemory<float> vector, long topK, string? indexNamespace = null, bool includeValues = false, bool includeMetadata = false);
    Task Update(object vector, string? indexNamespace = null);
    Task Upsert(ReadOnlyMemory<object> vectors, string? indexNamespace = null);
}

public interface ITransportFactory<T>
{
    abstract static T Create(string host, string apiKey);
}
