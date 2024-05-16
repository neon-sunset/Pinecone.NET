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
    
    public PineconeClient(string apiKey)
        : this(apiKey, new Uri($"https://api.pinecone.io"))
    {
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

    public async Task<IndexDetails[]> ListIndexes(CancellationToken cancellationToken = default)
    {
        var listIndexesResult = (ListIndexesResult?)await Http
            .GetFromJsonAsync("/indexes", typeof(ListIndexesResult), SerializerContext.Default, cancellationToken)
            .ConfigureAwait(false);

        return listIndexesResult?.Indexes ?? [];
    }

    public Task CreatePodBasedIndex(string name, uint dimension, Metric metric, string environment, string podType, long pods, CancellationToken cancellationToken = default)
        => CreateIndexAsync(new CreateIndexRequest
        {
            Name = name,
            Dimension = dimension,
            Metric = metric,
            Spec = new IndexSpec { Pod = new PodSpec { Environment = environment, PodType = podType, Pods = pods } }
        }, cancellationToken);

    public Task CreateServerlessIndex(string name, uint dimension, Metric metric, string cloud, string region, CancellationToken cancellationToken = default)
        => CreateIndexAsync(new CreateIndexRequest
        {
            Name = name,
            Dimension = dimension,
            Metric = metric,
            Spec = new IndexSpec { Serverless = new ServerlessSpec { Cloud = cloud, Region = region } }
        }, cancellationToken);

    private async Task CreateIndexAsync(CreateIndexRequest request, CancellationToken cancellationToken = default)
    {
        var response = await Http
            .PostAsJsonAsync("/indexes", request, SerializerContext.Default.CreateIndexRequest, cancellationToken)
            .ConfigureAwait(false);

        await response.CheckStatusCode().ConfigureAwait(false);
    }

    public Task<Index<GrpcTransport>> GetIndex(string name, CancellationToken cancellationToken = default) => GetIndex<GrpcTransport>(name, cancellationToken);

#if NET7_0_OR_GREATER
    public async Task<Index<TTransport>> GetIndex<TTransport>(string name, CancellationToken cancellationToken = default)
#else
    public async Task<Index<TTransport>> GetIndex<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TTransport>(string name, CancellationToken cancellationToken = default)
#endif
        where TTransport : ITransport<TTransport>
    {
        var response = (IndexDetails)(await Http
            .GetFromJsonAsync(
                $"/indexes/{UrlEncoder.Default.Encode(name)}",
                typeof(IndexDetails),
                SerializerContext.Default,
                cancellationToken)
            .ConfigureAwait(false) ?? throw new HttpRequestException("GetIndex request has failed."));

        // TODO: Host is optional according to the API spec: https://docs.pinecone.io/reference/api/control-plane/describe_index
        // but Transport requires it
        var host = response.Host!;
        var apiKey = Http.DefaultRequestHeaders.GetValues(Constants.RestApiKey).First();

        var index = new Index<TTransport>
        {
            Name = response.Name,
            Dimension = response.Dimension,
            Metric = response.Metric,
            Host = response.Host,
            Spec = response.Spec,
            Status = response.Status,
        };

#if NET7_0_OR_GREATER
        index.Transport = TTransport.Create(host, apiKey);
#else
        index.Transport = ITransport<TTransport>.Create(host, apiKey);
#endif

        return index;
    }

    public async Task ConfigureIndex(string name, int? replicas = null, string? podType = null, CancellationToken cancellationToken = default)
    {
        if (replicas is null && podType is null or [])
        {
            ThrowHelper.ThrowArgumentException(
                "At least one of the following parameters must be specified: replicas, podType");
        }

        var request = new ConfigureIndexRequest { Replicas = replicas, PodType = podType };
        var response = await Http
            .PatchAsJsonAsync(
                $"/indexes/{UrlEncoder.Default.Encode(name)}",
                request,
                SerializerContext.Default.ConfigureIndexRequest,
                cancellationToken)
            .ConfigureAwait(false);

        await response.CheckStatusCode().ConfigureAwait(false);
    }

    public async Task DeleteIndex(string name, CancellationToken cancellationToken = default) =>
        await (await Http.DeleteAsync($"/indexes/{UrlEncoder.Default.Encode(name)}", cancellationToken).ConfigureAwait(false))
            .CheckStatusCode()
            .ConfigureAwait(false);

    public async Task<CollectionDetails[]> ListCollections(CancellationToken cancellationToken = default)
    {
        var listCollectionsResult = (ListCollectionsResult?)await Http
            .GetFromJsonAsync("/collections", typeof(ListCollectionsResult), 
            SerializerContext.Default,
            cancellationToken)
            .ConfigureAwait(false);

        return listCollectionsResult?.Collections ?? [];
    }

    public async Task CreateCollection(string name, string source, CancellationToken cancellationToken = default)
    {
        var request = new CreateCollectionRequest { Name = name, Source = source };
        var response = await Http
            .PostAsJsonAsync("/collections", request, SerializerContext.Default.CreateCollectionRequest,
            cancellationToken)
            .ConfigureAwait(false);

        await response.CheckStatusCode().ConfigureAwait(false);
    }

    public async Task<CollectionDetails> DescribeCollection(string name, CancellationToken cancellationToken = default)
    {
        return await Http
            .GetFromJsonAsync(
                $"/collections/{UrlEncoder.Default.Encode(name)}",
                SerializerContext.Default.CollectionDetails, 
                cancellationToken)
            .ConfigureAwait(false) ?? ThrowHelpers.JsonException<CollectionDetails>();
    }

    public async Task DeleteCollection(string name, CancellationToken cancellationToken = default) =>
        await (await Http.DeleteAsync($"/collections/{UrlEncoder.Default.Encode(name)}", cancellationToken))
            .CheckStatusCode()
            .ConfigureAwait(false);

    public void Dispose() => Http.Dispose();
}
