using CommunityToolkit.Diagnostics;
using Google.Protobuf.Collections;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.Extensions.ObjectPool;
using Pinecone.Transport.Grpc;

namespace Pinecone.Transport;

public readonly record struct GrpcTransport : ITransport<GrpcTransport>
{
    private static readonly ObjectPool<RepeatedField<float>> ProtoVectorPool = ObjectPool.Create(new RepeatedFieldPolicy());

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

    public async Task<PineconeIndexStats> DescribeStats(IEnumerable<KeyValuePair<string, string>>? filter = null)
    {
        var request = new DescribeIndexStatsRequest();
        if (filter != null)
        {
            request.Filter = filter.ToProtoStruct();
        }

        using var response = Grpc.DescribeIndexStatsAsync(request, Auth);
        return (await response).ToPublicType();
    }

    public Task Query(
        ReadOnlyMemory<float> vector,
        long topK,
        string? indexNamespace = null,
        bool includeValues = false,
        bool includeMetadata = false)
    {
        // TODO: Figure out a way to avoid copying data to RepeatedField<float>.
        // TODO: Extra points for figuring out a way to directly write data to Grpc buffer.

        throw new NotImplementedException();
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
