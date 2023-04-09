using System.Text.Json.Serialization;
using Pinecone.Transport;

namespace Pinecone;

public partial record PineconeIndex<TTransport> : IDisposable
    where TTransport : struct, ITransport<TTransport>
{
    [JsonIgnore]
    internal TTransport Transport { get; set; }

    public Task<PineconeIndexStats> DescribeStats(
        IEnumerable<KeyValuePair<string, string>>? filter = null)
    {
        return Transport.DescribeStats(filter);
    }

    public Task<ScoredVector[]> Query(
        float[] vector,
        uint topK,
        string? indexNamespace = null,
        bool includeValues = false,
        bool includeMetadata = false)
    {
        return Transport.Query(
            vector, topK, indexNamespace, includeValues, includeMetadata);
    }

    // TODO: Add actual vectors
    public Task Upsert(ReadOnlyMemory<object> vectors, string? indexNamespace = null)
    {
        throw new NotImplementedException();
    }

    // TODO: Add actual vector
    public Task Update(object vector, string? indexNamespace = null)
    {
        throw new NotImplementedException();
    }

    public Task Fetch(IEnumerable<string> ids)
    {
        throw new NotImplementedException();
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