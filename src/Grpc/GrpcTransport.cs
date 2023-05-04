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

    public async Task<PineconeIndexStats> DescribeStats(MetadataMap? filter = null)
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
        uint topK,
        string? indexNamespace = null,
        bool includeValues = true,
        bool includeMetadata = false)
    {
        var request = new QueryRequest()
        {
            TopK = topK,
            Namespace = indexNamespace ?? "",
            IncludeMetadata = includeMetadata,
            IncludeValues = includeValues
        };

        if (string.IsNullOrWhiteSpace(id))
        {
            Guard.IsNotNull(values);
            request.Vector.OverwriteWith(values);
        }
        else
        {
            request.Id = id;
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

    public async Task<uint> Upsert(IEnumerable<PineconeVector> vectors, string? indexNamespace = null)
    {
        var request = new UpsertRequest { Namespace = indexNamespace ?? "" };
        request.Vectors.AddRange(vectors.Select(v => v.ToProtoVector()));

        using var call = Grpc.UpsertAsync(request, Auth);
        return (await call).UpsertedCount;
    }

    public async Task Update(PineconeVector vector, string? indexNamespace = null)
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

    public async Task<Dictionary<string, PineconeVector>> Fetch(
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

    public async Task Delete(IEnumerable<string> ids, string? indexNamespace = null)
    {
        var request = new DeleteRequest
        {
            Ids = { ids },
            DeleteAll = false,
            Namespace = indexNamespace ?? ""
        };

        using var call = Grpc.DeleteAsync(request, Auth);
        _ = await call;
    }

    public async Task Delete(MetadataMap filter, string? indexNamespace = null)
    {
        var request = new DeleteRequest
        {
            Filter = filter.ToProtoStruct(),
            DeleteAll = false,
            Namespace = indexNamespace ?? ""
        };

        using var call = Grpc.DeleteAsync(request, Auth);
        _ = await call;
    }

    public async Task DeleteAll(string? indexNamespace = null)
    {
        var request = new DeleteRequest
        {
            DeleteAll = true,
            Namespace = indexNamespace ?? ""
        };

        using var call = Grpc.DeleteAsync(request, Auth);
        _ = await call;
    }

    public void Dispose() => Channel.Dispose();
}
