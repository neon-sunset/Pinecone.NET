using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Json;
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

    public async Task<IndexName[]> ListIndexes()
    {
        var response = await Http
            .GetFromJsonAsync("/databases", SerializerContext.Default.StringArray)
            .ConfigureAwait(false);
        if (response is null or [])
        {
            return [];
        }

        var indexes = new IndexName[response.Length];
        foreach (var i in 0..response.Length)
        {
            indexes[i] = new(response[i]);
        }

        return indexes;
    }

    public Task CreateIndex(string name, uint dimension, Metric metric) =>
        CreateIndex(new IndexDetails { Name = name, Dimension = dimension, Metric = metric });

    public async Task CreateIndex(
        IndexDetails indexDetails,
        MetadataMap? metadataConfig = null,
        string? sourceCollection = null)
    {
        var request = CreateIndexRequest.From(indexDetails, metadataConfig, sourceCollection);
        var response = await Http
            .PostAsJsonAsync("/databases", request, SerializerContext.Default.CreateIndexRequest)
            .ConfigureAwait(false);

        await response.CheckStatusCode().ConfigureAwait(false);
    }

    public Task<Index<GrpcTransport>> GetIndex(IndexName name) => GetIndex<GrpcTransport>(name);

#if NET7_0_OR_GREATER
    public async Task<Index<TTransport>> GetIndex<TTransport>(IndexName name)
#else
    public async Task<Index<TTransport>> GetIndex<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TTransport>(IndexName name)
#endif
        where TTransport : ITransport<TTransport>
    {
        var response = await Http
            .GetFromJsonAsync(
                $"/databases/{name.Value}",
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

    public async Task ConfigureIndex(IndexName name, int replicas, string podType)
    {
        var request = new ConfigureIndexRequest { Replicas = replicas, PodType = podType };
        var response = await Http
            .PatchAsJsonAsync($"/databases/{name.Value}", request, SerializerContext.Default.ConfigureIndexRequest)
            .ConfigureAwait(false);

        await response.CheckStatusCode().ConfigureAwait(false);
    }

    public async Task DeleteIndex(IndexName name) =>
        await (await Http.DeleteAsync($"/databases/{name.Value}").ConfigureAwait(false))
            .CheckStatusCode()
            .ConfigureAwait(false);

    public async Task<CollectionName[]> ListCollections()
    {
        var response = await Http
            .GetFromJsonAsync("/collections", SerializerContext.Default.StringArray)
            .ConfigureAwait(false);
        if (response is null or [])
        {
            return [];
        }

        var collections = new CollectionName[response.Length];
        foreach (var i in 0..response.Length)
        {
            collections[i] = new(response[i]);
        }

        return collections;
    }

    public async Task CreateCollection(CollectionName name, IndexName source)
    {
        var request = new CreateCollectionRequest { Name = name, Source = source };
        var response = await Http
            .PostAsJsonAsync("/collections", request, SerializerContext.Default.CreateCollectionRequest)
            .ConfigureAwait(false);

        await response.CheckStatusCode().ConfigureAwait(false);
    }

    public async Task<CollectionDetails> DescribeCollection(CollectionName name)
    {
        return await Http
            .GetFromJsonAsync($"/collections/{name.Value}", SerializerContext.Default.CollectionDetails)
            .ConfigureAwait(false) ?? ThrowHelpers.JsonException<CollectionDetails>();
    }

    public async Task DeleteCollection(CollectionName name) =>
        await (await Http.DeleteAsync($"/collections/{name.Value}"))
            .CheckStatusCode()
            .ConfigureAwait(false);

    public void Dispose() => Http.Dispose();
}
