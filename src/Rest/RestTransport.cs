using CommunityToolkit.Diagnostics;

namespace Pinecone.Rest;

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

    public Task<PineconeIndexStats> DescribeStats(MetadataMap? filter = null)
    {
        // TODO: Implement filter (polymorphic) serialization
        throw new NotImplementedException();
    }

    public Task<ScoredVector[]> Query(string? id, float[]? vector, uint topK, string? indexNamespace = null, bool includeValues = false, bool includeMetadata = false)
    {
        throw new NotImplementedException();
    }

    public Task<uint> Upsert(IEnumerable<PineconeVector> vectors, string? indexNamespace = null)
    {
        throw new NotImplementedException();
    }

    public Task Delete(IEnumerable<string> ids, string? indexNamespace = null)
    {
        throw new NotImplementedException();
    }

    public Task Delete(MetadataMap filter, string? indexNamespace = null)
    {
        throw new NotImplementedException();
    }

    public Task DeleteAll(string? indexNamespace = null)
    {
        throw new NotImplementedException();
    }

    public Task<Dictionary<string, PineconeVector>> Fetch(
        IEnumerable<string> ids, string? indexNamespace = null)
    {
        throw new NotImplementedException();
    }

    public Task Update(PineconeVector vector, string? indexNamespace = null)
    {
        throw new NotImplementedException();
    }

    public void Dispose() => Http.Dispose();
}
