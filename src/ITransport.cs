using System.Diagnostics.CodeAnalysis;
using Pinecone.Grpc;
using Pinecone.Rest;

namespace Pinecone;

public interface ITransport<
#if NET7_0_OR_GREATER
    T> : IDisposable
#else
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T> : IDisposable
#endif
{
#if NET7_0_OR_GREATER
    static abstract T Create(string host, string apiKey);
#elif NET6_0
    static T Create(string host, string apiKey) => PineconeClient.CreateTransport<T>(host, apiKey);
#endif

    Task<IndexStats> DescribeStats(MetadataMap? filter = null, CancellationToken ct = default);
    Task<ScoredVector[]> Query(
        string? id,
        float[]? values,
        SparseVector? sparseValues,
        uint topK,
        MetadataMap? filter,
        string? indexNamespace,
        bool includeValues,
        bool includeMetadata,
        CancellationToken ct = default);
    Task<uint> Upsert(IEnumerable<Vector> vectors, string? indexNamespace = null, CancellationToken ct = default);
    Task Update(Vector vector, string? indexNamespace = null, CancellationToken ct = default);
    Task Update(
        string id,
        float[]? values = null,
        SparseVector? sparseValues = null,
        MetadataMap? metadata = null,
        string? indexNamespace = null,
        CancellationToken ct = default);
    Task<Dictionary<string, Vector>> Fetch(IEnumerable<string> ids, string? indexNamespace = null, CancellationToken ct = default);
    Task Delete(IEnumerable<string> ids, string? indexNamespace = null, CancellationToken ct = default);
    Task Delete(MetadataMap filter, string? indexNamespace = null, CancellationToken ct = default);
    Task DeleteAll(string? indexNamespace = null, CancellationToken ct = default);
}
