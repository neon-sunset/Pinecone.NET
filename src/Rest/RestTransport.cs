using System.Globalization;
using System.Net.Http.Json;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.Extensions.Logging;

namespace Pinecone.Rest;

public readonly record struct RestTransport : ITransport<RestTransport>
{
    readonly HttpClient Http;

    public RestTransport(string host, string apiKey, ILoggerFactory? loggerFactory)
    {
        ThrowHelpers.CheckNullOrWhiteSpace(host);
        ThrowHelpers.CheckNullOrWhiteSpace(apiKey);

        Http = new(loggerFactory?.CreateLoggingHandler()
            ?? new HttpClientHandler())
        { BaseAddress = new($"https://{host}") };
        Http.AddPineconeHeaders(apiKey);
    }

    public static RestTransport Create(string host, string apiKey, ILoggerFactory? loggerFactory) => new(host, apiKey, loggerFactory);

    public async Task<IndexStats> DescribeStats(MetadataMap? filter = null, CancellationToken ct = default)
    {
        var request = new DescribeStatsRequest { Filter = filter };
        using var response = await Http
            .PostAsJsonAsync("/describe_index_stats", request, RestTransportContext.Default.DescribeStatsRequest, ct)
            .ConfigureAwait(false);

        return await response.Content
            .ReadFromJsonAsync(RestTransportContext.Default.IndexStats, ct)
            .ConfigureAwait(false);
    }

    public async Task<ScoredVector[]> Query(
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
        if (id is null && values is null && sparseValues is null)
        {
            ThrowHelpers.ArgumentException(
                "At least one of the following parameters must be non-null: id, values, sparseValues.");
        }

        var request = new QueryRequest
        {
            Id = id,
            Vector = values,
            SparseVector = sparseValues,
            TopK = topK,
            Filter = filter,
            Namespace = indexNamespace ?? "",
            IncludeMetadata = includeMetadata,
            IncludeValues = includeValues,
        };

        using var response = await Http
            .PostAsJsonAsync("/query", request, RestTransportContext.Default.QueryRequest, ct)
            .ConfigureAwait(false);

        return (await response.Content
            .ReadFromJsonAsync(RestTransportContext.Default.QueryResponse, ct)
            .ConfigureAwait(false))
            .Matches ?? ThrowHelpers.JsonException<ScoredVector[]>();
    }

    public async Task<uint> Upsert(IEnumerable<Vector> vectors, string? indexNamespace = null, CancellationToken ct = default)
    {
        var request = new UpsertRequest
        {
            Vectors = vectors,
            Namespace = indexNamespace ?? ""
        };

        using var response = await Http
            .PostAsJsonAsync("/vectors/upsert", request, RestTransportContext.Default.UpsertRequest, ct)
            .ConfigureAwait(false);

        return (await response.Content
            .ReadFromJsonAsync(RestTransportContext.Default.UpsertResponse, ct)
            .ConfigureAwait(false)).UpsertedCount;
    }

    public async Task Update(Vector vector, string? indexNamespace = null, CancellationToken ct = default)
    {
        var request = new UpdateRequest
        {
            Id = vector.Id,
            Values = vector.Values,
            SparseValues = vector.SparseValues,
            SetMetadata = vector.Metadata,
            Namespace = indexNamespace ?? ""
        };

        using var response = await Http
            .PostAsJsonAsync("/vectors/update", request, RestTransportContext.Default.UpdateRequest, ct)
            .ConfigureAwait(false);

        await response.CheckStatusCode(ct).ConfigureAwait(false);
    }

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
            Values = values,
            SparseValues = sparseValues,
            SetMetadata = metadata,
            Namespace = indexNamespace ?? ""
        };

        using var response = await Http
            .PostAsJsonAsync("/vectors/update", request, RestTransportContext.Default.UpdateRequest, ct)
            .ConfigureAwait(false);

        await response.CheckStatusCode(ct).ConfigureAwait(false);
    }

    public async Task<(string[] VectorIds, string? PaginationToken, uint ReadUnits)> List(
        string? prefix,
        uint? limit,
        string? paginationToken,
        string? indexNamespace = null,
        CancellationToken ct = default)
    {
        var query = (StringBuilder?)null;
        void Append(string key, string? value)
        {
            if (value is null) return;
            query = query is null ? new("/vectors/list?") : query.Append('&');
            query.Append(key).Append('=').Append(UrlEncoder.Default.Encode(value));
        }
        Append("prefix", prefix);
        Append("limit", limit?.ToString(CultureInfo.InvariantCulture));
        Append("paginationToken", paginationToken);
        Append("namespace", indexNamespace);

        var response = await Http
            .GetFromJsonAsync(query?.ToString() ?? "/vectors/list", RestTransportContext.Default.ListResponse, ct)
            .ConfigureAwait(false);

        return (
            response.Vectors.Select(v => v.Id).ToArray(),
            response.Pagination?.Next,
            response.Usage.ReadUnits);
    }

    public async Task<Dictionary<string, Vector>> Fetch(
        IEnumerable<string> ids, string? indexNamespace = null, CancellationToken ct = default)
    {
        using var enumerator = ids.GetEnumerator();
        if (!enumerator.MoveNext())
        {
            throw new ArgumentException("Must contain at least one id", nameof(ids));
        }

        var addressBuilder = new StringBuilder("/vectors/fetch?ids=")
            .Append(UrlEncoder.Default.Encode(enumerator.Current));

        while (enumerator.MoveNext())
        {
            addressBuilder.Append("&ids=").Append(UrlEncoder.Default.Encode(enumerator.Current));
        }

        if (indexNamespace != null)
        {
            addressBuilder.Append("&namespace=").Append(UrlEncoder.Default.Encode(indexNamespace));
        }

        return (await Http
            .GetFromJsonAsync(addressBuilder.ToString(), RestTransportContext.Default.FetchResponse, ct)
            .ConfigureAwait(false)).Vectors;
    }

    public Task Delete(IEnumerable<string> ids, string? indexNamespace = null, CancellationToken ct = default)
    {
        return Delete(new()
        {
            Ids = ids as string[] ?? ids.ToArray(),
            DeleteAll = false,
            Namespace = indexNamespace
        }, ct);
    }

    public Task Delete(MetadataMap filter, string? indexNamespace = null, CancellationToken ct = default)
    {
        return Delete(new()
        {
            Filter = filter,
            DeleteAll = false,
            Namespace = indexNamespace
        }, ct);
    }

    public Task DeleteAll(string? indexNamespace = null, CancellationToken ct = default)
    {
        return Delete(new() { DeleteAll = true, Namespace = indexNamespace }, ct);
    }

    private async Task Delete(DeleteRequest request, CancellationToken ct)
    {
        using var response = await Http
            .PostAsJsonAsync("/vectors/delete", request, RestTransportContext.Default.DeleteRequest, ct)
            .ConfigureAwait(false);
        await response.CheckStatusCode(ct).ConfigureAwait(false);
    }

    public void Dispose() => Http.Dispose();
}
