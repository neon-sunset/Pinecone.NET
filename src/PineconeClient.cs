using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Json;
using System.Text.Encodings.Web;
using Microsoft.Extensions.Logging;
using Pinecone.Grpc;
using Pinecone.Rest;

namespace Pinecone;

/// <summary>
/// Main entry point for interacting with Pinecone. It is used to create, delete and modify indexes.
/// </summary>
public sealed class PineconeClient : IDisposable
{
    readonly HttpClient Http;
    readonly ILoggerFactory? LoggerFactory;

    /// <summary>
    /// Creates a new instance of the <see cref="PineconeClient" /> class.
    /// </summary>
    /// <param name="apiKey">API key used to connect to Pinecone.</param>
    /// <param name="loggerFactory">The logger factory to be used.</param>
    public PineconeClient(string apiKey, ILoggerFactory? loggerFactory = null)
        : this(apiKey, new Uri("https://api.pinecone.io"), loggerFactory)
    {
    }

    /// <summary>
    /// Creates a new instance of the <see cref="PineconeClient" /> class.
    /// </summary>
    /// <param name="apiKey">API key used to connect to Pinecone.</param>
    /// <param name="baseUrl">Url used to connect to Pinecone.</param>
    /// <param name="loggerFactory">The logger factory to be used.</param>
    public PineconeClient(string apiKey, Uri baseUrl, ILoggerFactory? loggerFactory = null)
    {
        ThrowHelpers.CheckNullOrWhiteSpace(apiKey);
        ThrowHelpers.CheckNull(baseUrl);

        Http = new(loggerFactory?.CreateLoggingHandler()
            ?? new HttpClientHandler())
        { BaseAddress = baseUrl };
        Http.AddPineconeHeaders(apiKey);
        LoggerFactory = loggerFactory;
    }

    /// <summary>
    /// Creates a new instance of the <see cref="PineconeClient" /> class.
    /// </summary>
    /// /// <param name="apiKey">API key used to connect to Pinecone.</param>
    /// /// <param name="client">HTTP client used to connect to Pinecone.</param>
    /// <param name="loggerFactory">The logger factory to be used.</param>
    public PineconeClient(string apiKey, HttpClient client, ILoggerFactory? loggerFactory = null)
    {
        ThrowHelpers.CheckNullOrWhiteSpace(apiKey);
        ThrowHelpers.CheckNull(client);

        Http = client;
        Http.AddPineconeHeaders(apiKey);
        LoggerFactory = loggerFactory;
    }

    /// <summary>
    /// Returns a list of indexes in the project.
    /// </summary>
    /// <param name="ct">A <see cref="CancellationToken" /> to observe while waiting for the task to complete.</param>
    /// <returns>List of index descriptions for all indexes in the project.</returns>
    public async Task<IndexDetails[]> ListIndexes(CancellationToken ct = default)
    {
        return (await Http
            .GetFromJsonAsync("/indexes", ClientContext.Default.ListIndexesResult, ct)
            .ConfigureAwait(false)).Indexes;
    }

    /// <summary>
    /// Creates a pod-based index. Pod-based indexes use pre-configured units of hardware.
    /// </summary>
    /// <param name="name">Name of the index.</param>
    /// <param name="dimension">The dimension of vectors stored in the index.</param>
    /// <param name="metric">The distance metric used for similarity search.</param>
    /// <param name="environment">The environment where the index is hosted. For free starter plan set the environment as "gcp-starter".</param>
    /// <param name="podType">The type of pod to use.  A string containing one of "s1", "p1", or "p2" appended with "." and one of "x1", "x2", "x4", or "x8".</param>
    /// <param name="pods">Number of pods to use. This should be equal to number of shards multiplied by the number of replicas.</param>
    /// <param name="shards">Number of shards to split the data across multiple pods.</param>
    /// <param name="replicas">Number of replicas. Replicas duplicate the index for greater availability and throughput.</param>
    /// <param name="ct">A <see cref="CancellationToken" /> to observe while waiting for the task to complete.</param>
    /// <returns></returns>
    public Task CreatePodBasedIndex(
        string name,
        uint dimension,
        Metric metric,
        string environment,
        string podType = "p1.x1",
        uint pods = 1,
        uint shards = 1,
        uint replicas = 1,
        CancellationToken ct = default)
    {
        return CreateIndex(new CreateIndexRequest
        {
            Name = name,
            Dimension = dimension,
            Metric = metric,
            Spec = new IndexSpec { Pod = new PodSpec { Environment = environment, PodType = podType, Pods = pods, Replicas = replicas, Shards = shards } }
        }, ct);
    }

    /// <summary>
    /// Creates a serverless index. Serverless indexes scale dynamically based on usage.
    /// </summary>
    /// <param name="name">Name of the index.</param>
    /// <param name="dimension">The dimension of vectors stored in the index.</param>
    /// <param name="metric">The distance metric used for similarity search.</param>
    /// <param name="cloud">The public cloud where the index will be hosted.</param>
    /// <param name="region">The region where the index will be created.</param>
    /// <param name="ct">A <see cref="CancellationToken" /> to observe while waiting for the task to complete.</param>
    /// <returns></returns>
    public Task CreateServerlessIndex(
        string name,
        uint dimension,
        Metric metric,
        string cloud,
        string region,
        CancellationToken ct = default)
    {
        return CreateIndex(new CreateIndexRequest
        {
            Name = name,
            Dimension = dimension,
            Metric = metric,
            Spec = new IndexSpec { Serverless = new ServerlessSpec { Cloud = cloud, Region = region } }
        }, ct);
    }

    private async Task CreateIndex(CreateIndexRequest request, CancellationToken ct = default)
    {
        using var response = await Http
            .PostAsJsonAsync("/indexes", request, ClientContext.Default.CreateIndexRequest, ct)
            .ConfigureAwait(false);
        await response.CheckStatusCode(ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Creates an <see cref="Index{RestTransport}"/> object describing the index. It is the main entry point for interacting with vectors. 
    /// It is used to upsert, query, fetch, update, delete and list vectors, as well as retrieving index statistics.
    /// </summary>
    /// <remarks>
    /// The <see cref="Index{T}"/> abstraction is thread-safe and can be shared across multiple threads.
    /// It is strongly recommended to cache and reuse the <see cref="Index{T}"/> object, for example by registering it as a singleton in a DI container.
    /// If not, make sure to dispose the <see cref="Index{T}"/> when it is no longer needed.
    /// </remarks>
    /// <param name="name">Name of the index to describe.</param>
    /// <param name="ct">A <see cref="CancellationToken" /> to observe while waiting for the task to complete.</param>
    /// <returns><see cref="Index{RestTransport}"/> describing the index.</returns>
    public Task<Index<RestTransport>> GetIndex(string name, CancellationToken ct = default) => GetIndex<RestTransport>(name, ct);

    /// <summary>
    /// Creates an <see cref="Index{TTransport}"/> object describing the index. It is a main entry point for interacting with vectors. 
    /// It is used to upsert, query, fetch, update, delete and list vectors, as well as retrieving index statistics.
    /// </summary>
    /// <remarks>
    /// The <see cref="Index{T}"/> abstraction is thread-safe and can be shared across multiple threads.
    /// It is strongly recommended to cache and reuse the <see cref="Index{T}"/> object, for example by registering it as a singleton in a DI container.
    /// If not, make sure to dispose the <see cref="Index{T}"/> when it is no longer needed.
    /// </remarks>
    /// <typeparam name="TTransport">The type of transport layer used, either <see cref="GrpcTransport"/> or <see cref="RestTransport"/>.</typeparam>
    /// <param name="name">Name of the index to describe.</param>
    /// <param name="ct">A <see cref="CancellationToken" /> to observe while waiting for the task to complete.</param>
    /// <returns><see cref="Index{TTransport}"/> describing the index.</returns>
#if NET7_0_OR_GREATER
    public async Task<Index<TTransport>> GetIndex<TTransport>(string name, CancellationToken ct = default)
#else
    public async Task<Index<TTransport>> GetIndex<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TTransport>(string name, CancellationToken ct = default)
#endif
        where TTransport : ITransport<TTransport>
    {
        var response = await Http
            .GetFromJsonAsync($"/indexes/{UrlEncoder.Default.Encode(name)}", ClientContext.Default.IndexDetails, ct)
            .ConfigureAwait(false) ?? throw new HttpRequestException("GetIndex request has failed.");

        var apiKey = Http.DefaultRequestHeaders.GetValues(Constants.RestApiKey).First();

        var index = new Index<TTransport>(LoggerFactory)
        {
            Name = response.Name,
            Dimension = response.Dimension,
            Metric = response.Metric,
            Host = response.Host,
            Spec = response.Spec,
            Status = response.Status,
#if NET7_0_OR_GREATER
            Transport = TTransport.Create(response.Host, apiKey, LoggerFactory)
#elif NET6_0
            Transport = ITransport<TTransport>.Create(response.Host, apiKey, LoggerFactory)
#else
            Transport = CreateTransport<TTransport>(response.Host, apiKey, LoggerFactory)
#endif
        };

        return index;
    }

#if !NET7_0_OR_GREATER
    internal static T CreateTransport<T>(string host, string apiKey, ILoggerFactory? loggerFactory)
    {
        if (typeof(T) == typeof(GrpcTransport))
        {
            return (T)(object)new GrpcTransport(host, apiKey, loggerFactory);
        }
        else if (typeof(T) == typeof(RestTransport))
        {
            return (T)(object)new RestTransport(host, apiKey, loggerFactory);
        }
        else
        {
            var instance = (T?)Activator.CreateInstance(typeof(T), host, apiKey);

            return instance ?? throw new InvalidOperationException($"Unable to create instance of {typeof(T)}");
        }
    }
#endif

    /// <summary>
    /// Specifies the pod type and number of replicas for an index. It applies to pod-based indexes only. Serverless indexes scale automatically based on usage.
    /// </summary>
    /// <param name="name">Name of the pod-based index to configure.</param>
    /// <param name="replicas">The new number or replicas.</param>
    /// <param name="podType">The new pod type.</param>
    /// <param name="ct">A <see cref="CancellationToken" /> to observe while waiting for the task to complete.</param>
    public async Task ConfigureIndex(
        string name,
        DeletionProtection? deletionProtection = null,
        int? replicas = null,
        string? podType = null,
        CancellationToken ct = default)
    {
        if (deletionProtection is null && replicas is null && podType is null or [])
        {
            ThrowHelpers.ArgumentException(
                "At least one of the following parameters must be specified: deletionProtection, replicas or podType.");
        }

        var request = new ConfigureIndexRequest
        {
            Spec = (replicas != null || podType != null) ? new()
            {
                Pod = new()
                {
                    Replicas = replicas,
                    PodType = podType
                }
            } : null,
            DeletionProtection = deletionProtection
        };

        using var response = await Http
            .PatchAsJsonAsync(
                $"/indexes/{UrlEncoder.Default.Encode(name)}",
                request,
                ClientContext.Default.ConfigureIndexRequest,
                ct)
            .ConfigureAwait(false);

        await response.CheckStatusCode(ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Deletes an existing index.
    /// </summary>
    /// <param name="name">Name of index to delete.</param>
    /// <param name="ct">A <see cref="CancellationToken" /> to observe while waiting for the task to complete.</param>
    public async Task DeleteIndex(string name, CancellationToken ct = default)
    {
        await (await Http.DeleteAsync($"/indexes/{UrlEncoder.Default.Encode(name)}", ct).ConfigureAwait(false))
            .CheckStatusCode(ct)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Returns a list of collections in the project.
    /// </summary>
    /// <param name="ct">A <see cref="CancellationToken" /> to observe while waiting for the task to complete.</param>
    /// <returns>List of collection descriptions for all collections in the project.</returns>
    public async Task<CollectionDetails[]> ListCollections(CancellationToken ct = default)
    {
        return (await Http
            .GetFromJsonAsync("/collections", ClientContext.Default.ListCollectionsResult, ct)
            .ConfigureAwait(false)).Collections ?? [];
    }

    /// <summary>
    /// Creates a new collection based on the source index.
    /// </summary>
    /// <param name="name">Name of the collection to create.</param>
    /// <param name="source">The name of the index to be used as the source for the collection.</param>
    /// <param name="ct">A <see cref="CancellationToken" /> to observe while waiting for the task to complete.</param>
    public async Task CreateCollection(string name, string source, CancellationToken ct = default)
    {
        var request = new CreateCollectionRequest { Name = name, Source = source };
        using var response = await Http
            .PostAsJsonAsync("/collections", request, ClientContext.Default.CreateCollectionRequest, ct)
            .ConfigureAwait(false);

        await response.CheckStatusCode(ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Gets a description of a collection.
    /// </summary>
    /// <param name="name">Name of the collection to describe.</param>
    /// <param name="ct">A <see cref="CancellationToken" /> to observe while waiting for the task to complete.</param>
    /// <returns>A <see cref="CollectionDetails"/> describing the collection.</returns>
    public async Task<CollectionDetails> DescribeCollection(string name, CancellationToken ct = default)
    {
        return await Http
            .GetFromJsonAsync(
                $"/collections/{UrlEncoder.Default.Encode(name)}",
                ClientContext.Default.CollectionDetails,
                ct)
            .ConfigureAwait(false) ?? ThrowHelpers.JsonException<CollectionDetails>();
    }

    /// <summary>
    /// Deletes an existing collection.
    /// </summary>
    /// <param name="name">Name of the collection to delete.</param>
    /// <param name="ct">A <see cref="CancellationToken" /> to observe while waiting for the task to complete.</param>
    public async Task DeleteCollection(string name, CancellationToken ct = default)
    {
        await (await Http.DeleteAsync($"/collections/{UrlEncoder.Default.Encode(name)}", ct))
            .CheckStatusCode(ct)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public void Dispose() => Http.Dispose();
}
