using Grpc.Core;
using Grpc.Net.Client;
using Grpc.Net.Client.Configuration;
using Microsoft.Extensions.Logging;

namespace Pinecone.Grpc;

public readonly record struct GrpcTransport : ITransport<GrpcTransport>
{
    readonly Metadata Metadata;
    readonly GrpcChannel Channel;
    readonly VectorService.VectorServiceClient Grpc;

    public GrpcTransport(string host, string apiKey, ILoggerFactory? loggerFactory = null)
    {
        ThrowHelpers.CheckNullOrWhiteSpace(host);
        ThrowHelpers.CheckNullOrWhiteSpace(apiKey);

        Metadata = new Metadata().WithPineconeProps(apiKey);
        Channel = GrpcChannel.ForAddress($"dns:///{host}", new()
        {
            Credentials = ChannelCredentials.SecureSsl,
#if NET6_0_OR_GREATER
            DisposeHttpClient = true,
            HttpHandler = new SocketsHttpHandler { EnableMultipleHttp2Connections = true },
#endif
            ServiceConfig = new() { LoadBalancingConfigs = { new RoundRobinConfig() } },
            LoggerFactory = loggerFactory
        });
        Grpc = new(Channel);
    }

    public static GrpcTransport Create(string host, string apiKey, ILoggerFactory? loggerFactory) => new(host, apiKey, loggerFactory);

    public async Task<IndexStats> DescribeStats(MetadataMap? filter = null, CancellationToken ct = default)
    {
        var request = new DescribeIndexStatsRequest();
        if (filter != null)
        {
            request.Filter = filter.ToProtoStruct();
        }

        using var call = Grpc.DescribeIndexStatsAsync(request, Metadata, cancellationToken: ct);
        
        return (await call.ConfigureAwait(false)).ToPublicType();
    }

    public async Task<Pinecone.ScoredVector[]> Query(
        string? id,
        ReadOnlyMemory<float>? values,
        SparseVector? sparseValues,
        uint topK,
        MetadataMap? filter,
        string? indexNamespace,
        bool includeValues,
        bool includeMetadata,
        CancellationToken ct = default)
    {
        var request = new QueryRequest()
        {
            TopK = topK,
            Filter = filter?.ToProtoStruct(),
            Namespace = indexNamespace ?? "",
            IncludeMetadata = includeMetadata,
            IncludeValues = includeValues
        };

        if (id != null)
        {
            request.Id = id;
        }
        else if (values != null || sparseValues != null)
        {
            request.Vector.OverwriteWith(values);
            request.SparseVector = sparseValues?.ToProtoSparseValues();
        }
        else
        {
            ThrowHelpers.ArgumentException(
                "At least one of the following parameters must be non-null: id, values, sparseValues.");
        }

        using var call = Grpc.QueryAsync(request, Metadata, cancellationToken: ct);
        var response = await call.ConfigureAwait(false);

        var matches = response.Matches;
        var vectors = new Pinecone.ScoredVector[response.Matches.Count];
        for (var i = 0; i < matches.Count; i++)
        {
            vectors[i] = matches[i].ToPublicType();
        }

        return vectors;
    }

    public async Task<uint> Upsert(IEnumerable<Pinecone.Vector> vectors, string? indexNamespace = null, CancellationToken ct = default)
    {
        var request = new UpsertRequest { Namespace = indexNamespace ?? "" };
        request.Vectors.AddRange(vectors.Select(v => v.ToProtoVector()));

        using var call = Grpc.UpsertAsync(request, Metadata, cancellationToken: ct);

        return (await call.ConfigureAwait(false)).UpsertedCount;
    }

    public Task Update(Pinecone.Vector vector, string? indexNamespace = null, CancellationToken ct = default) => Update(
        vector.Id,
        vector.Values,
        vector.SparseValues,
        vector.Metadata,
        indexNamespace,
        ct);

    public async Task Update(
        string id,
        ReadOnlyMemory<float>? values = null,
        SparseVector? sparseValues = null,
        MetadataMap? metadata = null,
        string? indexNamespace = null,
        CancellationToken ct = default)
    {        
        if (values is null && sparseValues is null && metadata is null)
        {
            ThrowHelpers.ArgumentException(
                "At least one of the following parameters must be non-null: values, sparseValues, metadata.");
        }

        var request = new UpdateRequest
        {
            Id = id,
            SparseValues = sparseValues?.ToProtoSparseValues(),
            SetMetadata = metadata?.ToProtoStruct(),
            Namespace = indexNamespace ?? ""
        };
        request.Values.OverwriteWith(values);

        using var call = Grpc.UpdateAsync(request, Metadata, cancellationToken: ct);
        _ = await call.ConfigureAwait(false);
    }

    public async Task<(string[] VectorIds, string? PaginationToken, uint ReadUnits)> List(
        string? prefix,
        uint? limit,
        string? paginationToken,
        string? indexNamespace = null,
        CancellationToken ct = default)
    {
        var request = new ListRequest
        {
            Prefix = prefix ?? "",
            Limit = limit ?? 0,
            PaginationToken = paginationToken ?? "",
            Namespace = indexNamespace ?? ""
        };

        using var call = Grpc.ListAsync(request, Metadata, cancellationToken: ct);
        var response = await call.ConfigureAwait(false);

        return (
            response.Vectors.Select(v => v.Id).ToArray(),
            response.Pagination.Next,
            response.Usage.ReadUnits);
    }

    public async Task<Dictionary<string, Pinecone.Vector>> Fetch(
        IEnumerable<string> ids, string? indexNamespace = null, CancellationToken ct = default)
    {
        var request = new FetchRequest
        {
            Ids = { ids },
            Namespace = indexNamespace ?? ""
        };

        using var call = Grpc.FetchAsync(request, Metadata, cancellationToken: ct);
        var response = await call.ConfigureAwait(false);

        return response.Vectors.ToDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value.ToPublicType());
    }

    public Task Delete(IEnumerable<string> ids, string? indexNamespace = null, CancellationToken ct = default) =>
        Delete(new()
        {
            Ids = { ids },
            DeleteAll = false,
            Namespace = indexNamespace ?? ""
        }, ct);

    public Task Delete(MetadataMap filter, string? indexNamespace = null, CancellationToken ct = default) =>
        Delete(new()
        {
            Filter = filter.ToProtoStruct(),
            DeleteAll = false,
            Namespace = indexNamespace ?? ""
        }, ct);

    public Task DeleteAll(string? indexNamespace = null, CancellationToken ct = default) =>
        Delete(new() { DeleteAll = true, Namespace = indexNamespace ?? "" }, ct);

    private async Task Delete(DeleteRequest request, CancellationToken ct)
    {
        using var call = Grpc.DeleteAsync(request, Metadata, cancellationToken: ct);
        _ = await call.ConfigureAwait(false);
    }

    public void Dispose() => Channel.Dispose();
}
