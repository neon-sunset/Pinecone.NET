using System.Net.Http.Json;
using System.Text;
using System.Text.Encodings.Web;
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

    public async Task<PineconeIndexStats> DescribeStats(MetadataMap? filter = null)
    {
        var request = new DescribeStatsRequest { Filter = filter };
        var response = await Http.PostAsJsonAsync(
            "/describe_index_stats", request, SerializerContext.Default.DescribeStatsRequest);

        await response.CheckStatusCode();
        return await response.Content.ReadFromJsonAsync(SerializerContext.Default.PineconeIndexStats)
            ?? ThrowHelpers.JsonException<PineconeIndexStats>();
    }

    public async Task<ScoredVector[]> Query(
        string? id,
        float[]? values,
        uint topK,
        string? indexNamespace = null,
        bool includeValues = false,
        bool includeMetadata = false)
    {
        QueryRequest request;
        if (string.IsNullOrWhiteSpace(id))
        {
            Guard.IsNotNull(values);
            request = new()
            {
                Vector = values,
                TopK = topK,
                Namespace = indexNamespace ?? "",
                IncludeMetadata = includeMetadata,
                IncludeValues = includeValues
            };
        }
        else
        {
            request = new()
            {
                Id = id,
                TopK = topK,
                Namespace = indexNamespace ?? "",
                IncludeMetadata = includeMetadata,
                IncludeValues = includeValues
            };
        }

        var response = await Http.PostAsJsonAsync(
            "/query", request, SerializerContext.Default.QueryRequest);

        await response.CheckStatusCode();
        return (await response.Content.ReadFromJsonAsync(SerializerContext.Default.QueryResponse))
            .Matches ?? ThrowHelpers.JsonException<ScoredVector[]>();
    }

    public async Task<uint> Upsert(IEnumerable<PineconeVector> vectors, string? indexNamespace = null)
    {
        var request = new UpsertRequest
        {
            Vectors = vectors as PineconeVector[] ?? vectors.ToArray(),
            Namespace = indexNamespace ?? ""
        };

        var response = await Http.PostAsJsonAsync("/vectors/upsert", request, SerializerContext.Default.UpsertRequest);

        await response.CheckStatusCode();
        return (await response.Content.ReadFromJsonAsync<UpsertResponse>()).UpsertedCount;
    }

    public async Task Update(PineconeVector vector, string? indexNamespace = null)
    {
        var request = UpdateRequest.From(vector, indexNamespace);
        var response = await Http.PostAsJsonAsync("/vectors/update", request, SerializerContext.Default.UpdateRequest);
        await response.CheckStatusCode();
    }

    public async Task<Dictionary<string, PineconeVector>> Fetch(
        IEnumerable<string> ids, string? indexNamespace = null)
    {
        using var enumerator = ids.GetEnumerator();
        if (!enumerator.MoveNext())
        {
            ThrowHelper.ThrowArgumentException(nameof(ids), "Must contain at least one id");
        }

        var addressBuilder = new StringBuilder("/vectors/fetch")
            .Append("?ids=")
            .Append(UrlEncoder.Default.Encode(enumerator.Current));

        while (enumerator.MoveNext())
        {
            addressBuilder.Append("&ids=").Append(UrlEncoder.Default.Encode(enumerator.Current));
        }

        return (await Http.GetFromJsonAsync(
            addressBuilder.ToString(), SerializerContext.Default.FetchResponse))
                .Vectors;
    }

    public Task Delete(IEnumerable<string> ids, string? indexNamespace = null) =>
        Delete(new()
        {
            Ids = ids as string[] ?? ids.ToArray(),
            DeleteAll = false,
            Namespace = indexNamespace ?? ""
        });

    public Task Delete(MetadataMap filter, string? indexNamespace = null) =>
        Delete(new()
        {
            Filter = filter,
            DeleteAll = false,
            Namespace = indexNamespace ?? ""
        });

    public Task DeleteAll(string? indexNamespace = null) =>
        Delete(new() { DeleteAll = true, Namespace = indexNamespace ?? "" });

    private async Task Delete(DeleteRequest request)
    {
        var response = await Http.PostAsJsonAsync("/vectors/delete", request, SerializerContext.Default.DeleteRequest);
        await response.CheckStatusCode();
    }

    public void Dispose() => Http.Dispose();
}
