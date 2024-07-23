using System.Diagnostics;
using System.Net.Http.Json;
using System.Text;
using System.Text.Encodings.Web;
using CommunityToolkit.Diagnostics;
using Microsoft.Extensions.Http.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Pinecone.Rest;

public readonly record struct RestTransport : ITransport<RestTransport>
{
    private readonly HttpClient Http;
    private readonly ILogger Logger;

    public RestTransport(string host, string apiKey, ILoggerFactory? loggerFactory)
    {
        Guard.IsNotNullOrWhiteSpace(host);
        Guard.IsNotNullOrWhiteSpace(apiKey);

        var httpClientLogger = loggerFactory?.CreateLogger<HttpClient>() ?? (ILogger)NullLogger.Instance;
        Http = new HttpClient(new LoggingHttpMessageHandler(httpClientLogger) { InnerHandler = new HttpClientHandler() }) { BaseAddress = new($"https://{host}") };
        Http.DefaultRequestHeaders.Add(Constants.RestApiKey, apiKey);
        Logger = loggerFactory?.CreateLogger<RestTransport>() ?? (ILogger)NullLogger.Instance;
    }

    public static RestTransport Create(string host, string apiKey, ILoggerFactory? loggerFactory) => new(host, apiKey, loggerFactory);

    public async Task<IndexStats> DescribeStats(MetadataMap? filter = null, CancellationToken ct = default)
    {
        var request = new DescribeStatsRequest { Filter = filter };
        var response = await Http
            .PostAsJsonAsync("/describe_index_stats", request, RestTransportContext.Default.DescribeStatsRequest, ct)
            .ConfigureAwait(false);

        await response.CheckStatusCode(Logger, "Describe stats", ct).ConfigureAwait(false);
        return await response.Content
            .ReadFromJsonAsync(RestTransportContext.Default.IndexStats, ct)
            .ConfigureAwait(false) ?? ThrowHelpers.JsonException<IndexStats>();
    }

    public async Task<ScoredVector[]> Query(
        string? id,
        float[]? values,
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
            var errorMessage = "At least one of the following parameters must be non-null: id, values, sparseValues.";
            Logger.OperationFailed("Query", errorMessage);
            ThrowHelper.ThrowArgumentException(errorMessage);
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

        var response = await Http
            .PostAsJsonAsync("/query", request, RestTransportContext.Default.QueryRequest, ct)
            .ConfigureAwait(false);

        await response.CheckStatusCode(Logger, "Query", ct).ConfigureAwait(false);
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

        var response = await Http
            .PostAsJsonAsync("/vectors/upsert", request, RestTransportContext.Default.UpsertRequest, ct)
            .ConfigureAwait(false);

        await response.CheckStatusCode(Logger, "Upsert", ct).ConfigureAwait(false);
        return (await response.Content
            .ReadFromJsonAsync(RestTransportContext.Default.UpsertResponse, ct)
            .ConfigureAwait(false)).UpsertedCount;
    }

    public async Task Update(Vector vector, string? indexNamespace = null, CancellationToken ct = default)
    {
        var request = UpdateRequest.From(vector, indexNamespace);
        Debug.Assert(request.Metadata is null);

        var response = await Http
            .PostAsJsonAsync("/vectors/update", request, RestTransportContext.Default.UpdateRequest, ct)
            .ConfigureAwait(false);
        await response.CheckStatusCode(Logger, "Update", ct).ConfigureAwait(false);
    }

    public async Task Update(
        string id,
        float[]? values = null,
        SparseVector? sparseValues = null,
        MetadataMap? metadata = null,
        string? indexNamespace = null,
        CancellationToken ct = default)
    {
        if (values is null && sparseValues is null && metadata is null)
        {
            var errorMessage = "At least one of the following parameters must be non-null: values, sparseValues, metadata.";
            Logger.OperationFailed("Update", errorMessage);
            ThrowHelper.ThrowArgumentException(errorMessage);
        }

        var request = new UpdateRequest
        {
            Id = id,
            Values = values!,
            SparseValues = sparseValues,
            SetMetadata = metadata,
            Namespace = indexNamespace ?? ""
        };

        var response = await Http
            .PostAsJsonAsync("/vectors/update", request, RestTransportContext.Default.UpdateRequest, ct)
            .ConfigureAwait(false);
        await response.CheckStatusCode(Logger, "Update", ct).ConfigureAwait(false);
    }

    public async Task<Dictionary<string, Vector>> Fetch(
        IEnumerable<string> ids, string? indexNamespace = null, CancellationToken ct = default)
    {
        using var enumerator = ids.GetEnumerator();
        if (!enumerator.MoveNext())
        {
            Logger.OperationFailed("Fetch", "ids must contain at least one id.");
            ThrowHelper.ThrowArgumentException(nameof(ids), "Must contain at least one id");
        }

        var addressBuilder = new StringBuilder("/vectors/fetch")
            .Append("?ids=")
            .Append(UrlEncoder.Default.Encode(enumerator.Current));

        while (enumerator.MoveNext())
        {
            addressBuilder.Append("&ids=").Append(UrlEncoder.Default.Encode(enumerator.Current));
        }

        return (await Http
            .GetFromJsonAsync(addressBuilder.ToString(), RestTransportContext.Default.FetchResponse, ct)
            .ConfigureAwait(false)).Vectors;
    }

    public Task Delete(IEnumerable<string> ids, string? indexNamespace = null, CancellationToken ct = default) =>
        Delete(new()
        {
            Ids = ids as string[] ?? ids.ToArray(),
            DeleteAll = false,
            Namespace = indexNamespace ?? ""
        }, ct);

    public Task Delete(MetadataMap filter, string? indexNamespace = null, CancellationToken ct = default) =>
        Delete(new()
        {
            Filter = filter,
            DeleteAll = false,
            Namespace = indexNamespace ?? ""
        }, ct);

    public Task DeleteAll(string? indexNamespace = null, CancellationToken ct = default) =>
        Delete(new() { DeleteAll = true, Namespace = indexNamespace ?? "" }, ct);

    private async Task Delete(DeleteRequest request, CancellationToken ct)
    {
        var response = await Http
            .PostAsJsonAsync("/vectors/delete", request, RestTransportContext.Default.DeleteRequest, ct)
            .ConfigureAwait(false);
        await response.CheckStatusCode(Logger, "Delete", ct).ConfigureAwait(false);
    }

    public void Dispose() => Http.Dispose();
}
