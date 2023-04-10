using CommunityToolkit.Diagnostics;
using Grpc.Core;
using Grpc.Net.Client;
using Pinecone.Transport.Grpc;

namespace Pinecone.Transport;

public readonly record struct GrpcTransport : ITransport<GrpcTransport>
{
    private readonly Metadata Auth;

    private readonly GrpcChannel Channel;

    private readonly VectorService.VectorServiceClient Grpc;

    public GrpcTransport(string host, string apiKey)
    {
        Guard.IsNotNullOrWhiteSpace(host);
        Guard.IsNotNullOrWhiteSpace(apiKey);

        Auth = new() { { "api-key", apiKey } };
        Channel = GrpcChannel.ForAddress($"https://{host}");
        Grpc = new(Channel);
    }

    public static GrpcTransport Create(string host, string apiKey) => new(host, apiKey);

    public async Task<PineconeIndexStats> DescribeStats(IEnumerable<KeyValuePair<string, MetadataValue>>? filter = null)
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
        bool includeValues = false,
        bool includeMetadata = false)
    {
        var request = new QueryRequest()
        {
            TopK = topK,
            Namespace = indexNamespace ?? "",
            IncludeMetadata = includeMetadata,
            IncludeValues = includeValues
        };
        request.Vector.OverwriteWith(values!); // TODO ID ^ VALUES case

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

    public Task Delete(IEnumerable<string> ids, string? indexNamespace = null)
    {
        throw new NotImplementedException();
    }

    public Task Delete(IDictionary<string, string> filter, string? indexNamespace = null)
    {
        throw new NotImplementedException();
    }

    public Task DeleteAll(string? indexNamespace = null)
    {
        throw new NotImplementedException();
    }

    public Task Fetch(IEnumerable<string> ids)
    {
        throw new NotImplementedException();
    }

    public Task Update(object vector, string? indexNamespace = null)
    {
        throw new NotImplementedException();
    }

    public Task Upsert(ReadOnlyMemory<object> vectors, string? indexNamespace = null)
    {
        throw new NotImplementedException();
    }

    public void Dispose() => Channel.Dispose();
}
