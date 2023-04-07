namespace Pinecone;

public partial record PineconeIndex
{
    public Task<PineconeIndexStats> DescribeStats(PineconeIndex indexDescription)
    {
        throw new NotImplementedException();
    }

    public Task Query(
        ReadOnlyMemory<float> vector,
        long topK,
        string? indexNamespace = null,
        bool includeValues = false,
        bool includeMetadata = false)
    {
        throw new NotImplementedException();
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
}