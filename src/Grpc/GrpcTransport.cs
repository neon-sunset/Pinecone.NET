using CommunityToolkit.Diagnostics;
using Grpc.Core;
using Grpc.Net.Client;

namespace Pinecone.Grpc;

public readonly record struct GrpcTransport : ITransport<GrpcTransport>
{
    private readonly Metadata Auth;

    private readonly GrpcChannel Channel;

    private readonly VectorService.VectorServiceClient Grpc;

    public GrpcTransport(string host, string apiKey)
    {
        Guard.IsNotNullOrWhiteSpace(host);
        Guard.IsNotNullOrWhiteSpace(apiKey);

        Auth = new() { { Constants.GrpcApiKey, apiKey } };
        Channel = GrpcChannel.ForAddress($"https://{host}");
        Grpc = new(Channel);
    }

    public static GrpcTransport Create(string host, string apiKey) => new(host, apiKey);

    public async Task<IndexStats> DescribeStats(MetadataMap? filter = null)
    {
        var request = new DescribeIndexStatsRequest();
        if (filter != null)
        {
            request.Filter = filter.ToProtoStruct();
        }

        using var call = Grpc.DescribeIndexStatsAsync(request, Auth);
        return (await call).ToPublicType();
    }

    public async Task<ScoredVector[]> Query(
        string? id,
        float[]? values,
        SparseVector? sparseValues,
        uint topK,
        MetadataMap? filter,
        string? indexNamespace,
        bool includeValues,
        bool includeMetadata)
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
            ThrowHelper.ThrowArgumentException(
                nameof(values), "At least one of the following parameters must be non-null: id, values, sparseValues");
        }

        using var call = Grpc.QueryAsync(request, Auth);
        var response = await call;

        var matches = response.Matches;
        var vectors = new ScoredVector[response.Matches.Count];
        foreach (var i in 0..matches.Count)
        {
            vectors[i] = matches[i].ToPublicType();
        }

        return vectors;
    }

    public async Task<uint> Upsert(IEnumerable<Vector> vectors, string? indexNamespace = null)
    {
        var request = new UpsertRequest { Namespace = indexNamespace ?? "" };
        request.Vectors.AddRange(vectors.Select(v => v.ToProtoVector()));

        using var call = Grpc.UpsertAsync(request, Auth);
        return (await call).UpsertedCount;
    }

    public async Task Update(Vector vector, string? indexNamespace = null)
    {
        var request = new UpdateRequest
        {
            Id = vector.Id,
            SparseValues = vector.SparseValues?.ToProtoSparseValues(),
            SetMetadata = vector.Metadata?.ToProtoStruct(),
            Namespace = indexNamespace ?? ""
        };
        request.Values.OverwriteWith(vector.Values);

        using var call = Grpc.UpdateAsync(request, Auth);
        _ = await call;
    }

    public async Task<Dictionary<string, Vector>> Fetch(
        IEnumerable<string> ids, string? indexNamespace = null)
    {
        var request = new FetchRequest
        {
            Ids = { ids },
            Namespace = indexNamespace ?? ""
        };

        using var call = Grpc.FetchAsync(request, Auth);
        var response = await call;

        return response.Vectors.ToDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value.ToPublicType());
    }

    public Task Delete(IEnumerable<string> ids, string? indexNamespace = null) =>
        Delete(new()
        {
            Ids = { ids },
            DeleteAll = false,
            Namespace = indexNamespace ?? ""
        });

    public Task Delete(MetadataMap filter, string? indexNamespace = null) =>
        Delete(new()
        {
            Filter = filter.ToProtoStruct(),
            DeleteAll = false,
            Namespace = indexNamespace ?? ""
        });

    public Task DeleteAll(string? indexNamespace = null) =>
        Delete(new() { DeleteAll = true, Namespace = indexNamespace ?? "" });

    private async Task Delete(DeleteRequest request)
    {
        using var call = Grpc.DeleteAsync(request, Auth);
        _ = await call;
    }

    public void Dispose() => Channel.Dispose();
}
