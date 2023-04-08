using CommunityToolkit.Diagnostics;

namespace Pinecone.Transport;

public readonly record struct RestTransport : ITransport<RestTransport>
{
    private readonly HttpClient Http;

    public RestTransport(string host, string apiKey)
    {
        Guard.IsNotNullOrWhiteSpace(host);
        Guard.IsNotNullOrWhiteSpace(apiKey);

        Http = new HttpClient { BaseAddress = new($"https://{host}") };
        Http.DefaultRequestHeaders.Add(Constants.RestApiKey, apiKey);
    }

    public static RestTransport Create(string host, string apiKey) => new(host, apiKey);

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

    public Task<PineconeIndexStats> DescribeStats(IEnumerable<KeyValuePair<string, string>>? filter = null)
    {
        throw new NotImplementedException();
    }

    public Task Fetch(IEnumerable<string> ids)
    {
        throw new NotImplementedException();
    }

    public Task Query(ReadOnlyMemory<float> vector, long topK, string? indexNamespace = null, bool includeValues = false, bool includeMetadata = false)
    {
        throw new NotImplementedException();
    }

    public Task Update(object vector, string? indexNamespace = null)
    {
        throw new NotImplementedException();
    }

    public Task Upsert(ReadOnlyMemory<object> vectors, string? indexNamespace = null)
    {
        throw new NotImplementedException();
    }

    public void Dispose() => Http.Dispose();
}
