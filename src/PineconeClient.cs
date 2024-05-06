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
    private readonly string? _legacyEnvironment;
    
    public PineconeClient(string apiKey)
        : this(apiKey, new Uri($"https://api.pinecone.io"))
    {
    }

    public PineconeClient(string apiKey, string environment)
    {
        Guard.IsNotNullOrWhiteSpace(apiKey);
        Guard.IsNotNullOrWhiteSpace(environment);

        Http = new() { BaseAddress = new Uri($"https://controller.{environment}.pinecone.io") };
        Http.DefaultRequestHeaders.Add("Api-Key", apiKey);
        _legacyEnvironment = environment;
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

    public async Task<IndexDetails[]> ListIndexes()
    {
        var listIndexesResult = (ListIndexesResult?)await Http
            .GetFromJsonAsync("/indexes", typeof(ListIndexesResult), SerializerContext.Default)
            .ConfigureAwait(false);

        return listIndexesResult?.Indexes ?? [];
    }

    public Task CreatePodBasedIndex(string name, uint dimiension, Metric metric, string environment, string podType, long pods)
        => CreateIndexAsync(new CreateIndexRequest
        {
            Name = name,
            Dimension = dimiension,
            Metric = metric,
            Spec = new IndexSpec { Pod = new PodSpec { Environment = environment, PodType = podType, Pods = pods } }
        });

    public Task CreateServerlessIndex(string name, uint dimiension, Metric metric, string cloud, string region)
        => CreateIndexAsync(new CreateIndexRequest
        {
            Name = name,
            Dimension = dimiension,
            Metric = metric,
            Spec = new IndexSpec { Serverless = new ServerlessSpec { Cloud = cloud, Region = region } }
        });

    private async Task CreateIndexAsync(CreateIndexRequest request)
    {
        var response = await Http
            .PostAsJsonAsync("/indexes", request, SerializerContext.Default.CreateIndexRequest)
            .ConfigureAwait(false);

        await response.CheckStatusCode().ConfigureAwait(false);
    }

    [Obsolete($"Use '{nameof(CreateServerlessIndex)}' or '{nameof(CreatePodBasedIndex)}' methods instead.")]
    public Task CreateIndex(string name, uint dimension, Metric metric) 
        => _legacyEnvironment is not null
            ? CreatePodBasedIndex(name, dimension, metric, _legacyEnvironment, "starter", 1)
            : throw new InvalidOperationException($"Use '{nameof(CreateServerlessIndex)}' or '{nameof(CreatePodBasedIndex)}' methods instead.");

    public Task<Index<GrpcTransport>> GetIndex(string name) => GetIndex<GrpcTransport>(name);

#if NET7_0_OR_GREATER
    public async Task<Index<TTransport>> GetIndex<TTransport>(string name)
#else
    public async Task<Index<TTransport>> GetIndex<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TTransport>(string name)
#endif
        where TTransport : ITransport<TTransport>
    {
        var response = (IndexDetails)(await Http
            .GetFromJsonAsync(
                $"/indexes/{UrlEncoder.Default.Encode(name)}",
                typeof(IndexDetails),
                SerializerContext.Default)
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
                $"/indexes/{UrlEncoder.Default.Encode(name)}",
                request,
                SerializerContext.Default.ConfigureIndexRequest)
            .ConfigureAwait(false);

        await response.CheckStatusCode().ConfigureAwait(false);
    }

    public async Task DeleteIndex(string name) =>
        await (await Http.DeleteAsync($"/indexes/{UrlEncoder.Default.Encode(name)}").ConfigureAwait(false))
            .CheckStatusCode()
            .ConfigureAwait(false);

    public async Task<CollectionDetails[]> ListCollections()
    {
        var listCollectionsResult = (ListCollectionsResult?)await Http
            .GetFromJsonAsync("/collections", typeof(ListCollectionsResult), SerializerContext.Default)
            .ConfigureAwait(false);

        return listCollectionsResult?.Collections ?? [];
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
