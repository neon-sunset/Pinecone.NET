using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Json;
using System.Text.Encodings.Web;
using CommunityToolkit.Diagnostics;
using Pinecone.Grpc;
using Pinecone.Rest;

namespace Pinecone;

public sealed class PineconeClient : IDisposable
{
    private readonly HttpClient Http;

    public PineconeClient(string apiKey, string environment)
    {
        Guard.IsNotNullOrWhiteSpace(apiKey);
        Guard.IsNotNullOrWhiteSpace(environment);

        Http = new() { BaseAddress = new Uri($"https://controller.{environment}.pinecone.io") };
        Http.DefaultRequestHeaders.Add("Api-Key", apiKey);
    }

    public PineconeClient(string apiKey, Uri baseUrl)
    {
        Guard.IsNotNullOrWhiteSpace(apiKey);
        Guard.IsNotNull(baseUrl);

        Http = new() { BaseAddress = baseUrl };
        Http.DefaultRequestHeaders.Add("Api-Key", apiKey);
    }

    public PineconeClient(string apiKey, HttpClient client)
    {
        Guard.IsNotNullOrWhiteSpace(apiKey);
        Guard.IsNotNull(client);

        Http = client;
        Http.DefaultRequestHeaders.Add("Api-Key", apiKey);
    }

    public async Task<string[]> ListIndexes()
    {
        var indexes = await Http
            .GetFromJsonAsync("/databases", SerializerContext.Default.StringArray)
            .ConfigureAwait(false);

        return indexes ?? [];
    }

    public Task CreateIndex(string name, uint dimension, Metric metric) =>
        CreateIndex(new IndexDetails { Name = name, Dimension = dimension, Metric = metric });

    public async Task CreateIndex(IndexDetails indexDetails, string? sourceCollection = null)
    {
        var request = CreateIndexRequest.From(indexDetails, sourceCollection);
        var response = await Http
            .PostAsJsonAsync("/databases", request, SerializerContext.Default.CreateIndexRequest)
            .ConfigureAwait(false);

        await response.CheckStatusCode().ConfigureAwait(false);
    }

    public Task<Index<GrpcTransport>> GetIndex(string name) => GetIndex<GrpcTransport>(name);

#if NET7_0_OR_GREATER
    public async Task<Index<TTransport>> GetIndex<TTransport>(string name)
#else
    public async Task<Index<TTransport>> GetIndex<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TTransport>(string name)
#endif
        where TTransport : ITransport<TTransport>
    {
        var response = await Http
            .GetFromJsonAsync(
                $"/databases/{UrlEncoder.Default.Encode(name)}",
                typeof(Index<TTransport>),
                SerializerContext.Default)
            .ConfigureAwait(false) ?? throw new HttpRequestException("GetIndex request has failed.");

        var index = (Index<TTransport>)response;
        var host = index.Status.Host;
        var apiKey = Http.DefaultRequestHeaders.GetValues(Constants.RestApiKey).First();

#if NET7_0_OR_GREATER
        index.Transport = TTransport.Create(host, apiKey);
#else
        index.Transport = ITransport<TTransport>.Create(host, apiKey);
#endif
        return index;
    }

    public async Task ConfigureIndex(string name, int? replicas = null, string? podType = null)
    {
        if (replicas is null && podType is null or [])
        {
            ThrowHelper.ThrowArgumentException(
                "At least one of the following parameters must be specified: replicas, podType");
        }

        var request = new ConfigureIndexRequest { Replicas = replicas, PodType = podType };
        var response = await Http
            .PatchAsJsonAsync(
                $"/databases/{UrlEncoder.Default.Encode(name)}",
                request,
                SerializerContext.Default.ConfigureIndexRequest)
            .ConfigureAwait(false);

        await response.CheckStatusCode().ConfigureAwait(false);
    }

    public async Task DeleteIndex(string name) =>
        await (await Http.DeleteAsync($"/databases/{UrlEncoder.Default.Encode(name)}").ConfigureAwait(false))
            .CheckStatusCode()
            .ConfigureAwait(false);

    public async Task<string[]> ListCollections()
    {
        var collections = await Http
            .GetFromJsonAsync("/collections", SerializerContext.Default.StringArray)
            .ConfigureAwait(false);

        return collections ?? [];
    }

    public async Task CreateCollection(string name, string source)
    {
        var request = new CreateCollectionRequest { Name = name, Source = source };
        var response = await Http
            .PostAsJsonAsync("/collections", request, SerializerContext.Default.CreateCollectionRequest)
            .ConfigureAwait(false);

        await response.CheckStatusCode().ConfigureAwait(false);
    }

    public async Task<CollectionDetails> DescribeCollection(string name)
    {
        return await Http
            .GetFromJsonAsync(
                $"/collections/{UrlEncoder.Default.Encode(name)}",
                SerializerContext.Default.CollectionDetails)
            .ConfigureAwait(false) ?? ThrowHelpers.JsonException<CollectionDetails>();
    }

    public async Task DeleteCollection(string name) =>
        await (await Http.DeleteAsync($"/collections/{UrlEncoder.Default.Encode(name)}"))
            .CheckStatusCode()
            .ConfigureAwait(false);

    public void Dispose() => Http.Dispose();
}
