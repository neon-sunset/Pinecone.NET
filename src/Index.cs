using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;

namespace Pinecone;

// Contract
public sealed partial record Index<
#if NET6_0
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
#endif
    TTransport> where TTransport : ITransport<TTransport>
{
    /// <summary>
    /// Name of the index.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// The dimension of the vectors stored in the index.
    /// </summary>
    public required uint Dimension { get; init; }

    /// <summary>
    /// The distance metric to be used for similarity search.
    /// </summary>
    public required Metric Metric { get; init; }

    /// <summary>
    /// The URL address where the index is hosted.
    /// </summary>
    public required string Host { get; init; }

    /// <summary>
    /// Additional information about the index.
    /// </summary>
    public required IndexSpec Spec { get; init; }

    /// <summary>
    /// The current status of the index.
    /// </summary>
    public required IndexStatus Status { get; init; }
}

// Implementation

/// <summary>
/// An object used for interacting with vectors. It is used to upsert, query, fetch, update, delete and list vectors, as well as retrieving index statistics.
/// </summary>
/// <remarks>
/// The <see cref="Index{T}"/> abstraction is thread-safe and can be shared across multiple threads.
/// It is strongly recommended to cache and reuse the <see cref="Index{T}"/> object, for example by registering it as a singleton in a DI container.
/// If not, make sure to dispose the <see cref="Index{T}"/> when it is no longer needed.
/// </remarks>
/// <typeparam name="TTransport">The type of transport layer used.</typeparam>
public sealed partial record Index<TTransport> : IDisposable
    where TTransport : ITransport<TTransport>
{
#if NET6_0_OR_GREATER
    const int BatchParallelism = 10;

    readonly ILogger? Logger;
#endif

    /// <summary>
    /// Creates a new instance of the <see cref="Index{TTransport}" /> class.
    /// </summary>
    /// <param name="loggerFactory">The logger factory to be used.</param>
    public Index(ILoggerFactory? loggerFactory = null)
    {
#if NET6_0_OR_GREATER
        Logger = loggerFactory?.CreateLogger<Index<TTransport>>();
#endif
    }

    /// <summary>
    /// The transport layer.
    /// </summary>
    public required TTransport Transport { private get; init; }

    /// <summary>
    /// Returns statistics describing the contents of an index, including the vector count per namespace and the number of dimensions, and the index fullness.
    /// </summary>
    /// <param name="filter">The operation only returns statistics for vectors that satisfy the filter.</param>
    /// <returns>An <see cref="IndexStats"/> object containing index statistics.</returns>
    public Task<IndexStats> DescribeStats(MetadataMap? filter = null, CancellationToken ct = default)
    {
        return Transport.DescribeStats(filter, ct);
    }

    /// <summary>
    /// Searches an index using the values of a vector with specified ID. It retrieves the IDs of the most similar items, along with their similarity scores.
    /// </summary>
    /// <remarks>
    /// Query by ID uses Approximate Nearest Neighbor, which doesn't guarantee the input vector to appear in the results. To ensure that, use the Fetch operation instead.
    /// <para/>
    /// If you do not need to include vector values in the response, set <paramref name="includeValues"/> to <c>false</c> to reduce the response size and read units consumption for the operation.
    /// </remarks>
    /// <param name="id">The unique ID of the vector to be used as a query vector.</param>
    /// <param name="topK">The number of results to return for each query.</param>
    /// <param name="filter">The filter to apply.</param>
    /// <param name="indexNamespace">Namespace to query from. If no namespace is provided, the operation applies to all namespaces.</param>
    /// <param name="includeValues">Indicates whether vector values are included in the response.</param>
    /// <param name="includeMetadata">Indicates whether metadata is included in the response as well as the IDs.</param>
    /// <returns></returns>
    public Task<ScoredVector[]> Query(
        string id,
        uint topK,
        MetadataMap? filter = null,
        string? indexNamespace = null,
        bool includeValues = true,
        bool includeMetadata = false,
        CancellationToken ct = default)
    {
        return Transport.Query(
            id: id,
            values: null,
            sparseValues: null,
            topK: topK,
            filter: filter,
            indexNamespace: indexNamespace,
            includeValues: includeValues,
            includeMetadata: includeMetadata,
            ct: ct);
    }

    /// <summary>
    /// Searches an index using the specified vector values. It retrieves the IDs of the most similar items, along with their similarity scores.
    /// </summary>
    /// <remarks>
    /// If you do not need to include vector values in the response, set <paramref name="includeValues"/> to <c>false</c> to reduce the response size and read units consumption for the operation.
    /// </remarks>
    /// <param name="values">The query vector. This should be the same length as the dimension of the index being queried.</param>
    /// <param name="sparseValues">Vector sparse data. Represented as a list of indices and a list of corresponded values, which must be with the same length.</param>
    /// <param name="topK">The number of results to return for each query.</param>
    /// <param name="filter">The filter to apply.</param>
    /// <param name="indexNamespace">Namespace to query from. If no namespace is provided, the operation applies to all namespaces.</param>
    /// <param name="includeValues">Indicates whether vector values are included in the response.</param>
    /// <param name="includeMetadata">Indicates whether metadata is included in the response as well as the IDs.</param>
    public Task<ScoredVector[]> Query(
        ReadOnlyMemory<float> values,
        uint topK,
        MetadataMap? filter = null,
        SparseVector? sparseValues = null,
        string? indexNamespace = null,
        bool includeValues = true,
        bool includeMetadata = false,
        CancellationToken ct = default)
    {
        return Transport.Query(
            id: null,
            values: values,
            sparseValues: sparseValues,
            topK: topK,
            filter: filter,
            indexNamespace: indexNamespace,
            includeValues: includeValues,
            includeMetadata: includeMetadata,
            ct: ct);
    }

    /// <summary>
    /// Writes vectors into the index. If a new value is provided for an existing vector ID, it will overwrite the previous value.
    /// </summary>
    /// <remarks>
    /// If the sequence of vectors is countable and greater than or equal to 400, it will be batched and the batches
    /// will be upserted in parallel. The default batch size is 100 and the default parallelism is 20.
    /// </remarks>
    /// <param name="vectors">A collection of <see cref="Vector"/> objects to upsert.</param>
    /// <param name="indexNamespace">Namespace to write the vector to. If no namespace is provided, the operation applies to all namespaces.</param>
    /// <returns>The number of vectors upserted.</returns>
    /// <exception cref="ParallelUpsertException">
    /// Thrown when the countable sequence of vectors is above the threshold batch size which initiates automatic parallel upserting
    /// and one or more batch upsert operations have failed.
    /// The exception contains the number of successfully upserted vectors, the IDs and exceptions of the batches that failed to upsert.
    /// This applies to .NET 6.0 or newer versions of this library.
    /// </exception>
    public Task<uint> Upsert(
        IEnumerable<Vector> vectors,
        string? indexNamespace = null,
        CancellationToken ct = default)
    {
#if NET6_0_OR_GREATER
        var batchSize = GetBatchSize(); 

        if (vectors.TryGetNonEnumeratedCount(out var count) && count > batchSize)
        {
            return Upsert(vectors, batchSize, BatchParallelism, indexNamespace, ct);
        }
#endif
        return Transport.Upsert(vectors, indexNamespace, ct);
    }

#if NET6_0_OR_GREATER
    /// <summary>
    /// Writes vectors into the index as batches in parallel. If a new value is provided for an existing vector ID, it will overwrite the previous value.
    /// </summary>
    /// <param name="vectors">A collection of <see cref="Vector"/> objects to upsert.</param>
    /// <param name="batchSize">The number of vectors to upsert in each batch.</param>
    /// <param name="parallelism">The maximum number of batches to process in parallel.</param>
    /// <param name="indexNamespace">Namespace to write the vector to. If no namespace is provided, the operation applies to all namespaces.</param>
    /// <returns>The number of vectors upserted.</returns>
    /// <exception cref="ParallelUpsertException">
    /// Thrown when one or more batch upsert operations fail.
    /// The exception contains the number of successfully upserted vectors, the IDs and exceptions of the batches that failed to upsert.
    /// </exception>
    public async Task<uint> Upsert(
        IEnumerable<Vector> vectors,
        int batchSize,
        int parallelism,
        string? indexNamespace = null,
        CancellationToken ct = default)
    {
        ThrowHelpers.CheckGreaterThan(batchSize, 0);
        ThrowHelpers.CheckGreaterThan(parallelism, 0);

        var upserted = 0u;
        var batches = vectors.Chunk(batchSize);
        var options = new ParallelOptions
        {
            // We are intentionally not assigning the CancellationToken here
            // to allow batch exception aggregation logic to execute.
            // Otherwise, avoiding the race condition where the token is raised
            // before any of the workers reaches the catch block would be
            // tricky without making the code ugly.
            MaxDegreeOfParallelism = parallelism
        };
        
        var exceptions = (ConcurrentStack<Exception>?)null;
        var failedVectorBatches = (ConcurrentStack<string[]>?)null;

        Logger?.ParallelOperationStarted(batchSize, parallelism);
        await Parallel.ForEachAsync(batches, options, async (batch, _) =>
        {
            try
            {
                Interlocked.Add(ref upserted, await Transport.Upsert(batch, indexNamespace, ct));
            }
            catch (Exception ex)
            {
                if (exceptions is null) Interlocked.CompareExchange(ref exceptions, [], null);
                exceptions.Push(ex);

                if (failedVectorBatches is null) Interlocked.CompareExchange(ref failedVectorBatches, [], null);
                failedVectorBatches.Push(batch.Select(v => v.Id).ToArray());
            }
        });

        if (exceptions != null)
        {
            var message = "One or more exceptions have occurred. " +
                $"Successfully upserted {upserted} vectors. Batches failed: {exceptions.Count}.";

            Logger?.ParallelOperationFailed(message);
            throw new ParallelUpsertException(
                upserted,
                message,
                failedVectorBatches!.SelectMany(b => b).ToArray(),
                [..exceptions]);
        }

        Logger?.ParallelOperationCompleted($"Upserted {upserted} vectors.");
        return upserted;
    }
#endif

    /// <summary>
    /// Updates a vector using the <see cref="Vector"/> object.
    /// </summary>
    /// <param name="vector"><see cref="Vector"/> object containing updated information.</param>
    /// <param name="indexNamespace">Namespace to update the vector from. If no namespace is provided, the operation applies to all namespaces.</param>
    public Task Update(Vector vector, string? indexNamespace = null, CancellationToken ct = default)
    {
        return Transport.Update(vector, indexNamespace, ct);
    }

    /// <summary>
    /// Updates a vector.
    /// </summary>
    /// <param name="id">The ID of the vector to update.</param>
    /// <param name="values">New vector values.</param>
    /// <param name="sparseValues">New vector sparse data. </param>
    /// <param name="metadata">New vector metadata.</param>
    /// <param name="indexNamespace">Namespace to update the vector from. If no namespace is provided, the operation applies to all namespaces.</param>
    public Task Update(
        string id,
        ReadOnlyMemory<float>? values = null,
        SparseVector? sparseValues = null,
        MetadataMap? metadata = null,
        string? indexNamespace = null,
        CancellationToken ct = default)
    {
        return Transport.Update(id, values, sparseValues, metadata, indexNamespace, ct);
    }

    /// <summary>
    /// An asynchronous iterator that lists vector IDs in the index using specified page size, prefix, and read units threshold.
    /// The iterator terminates when all vectors have been listed or the read units threshold has been reached (if specified).
    /// </summary>
    /// <param name="prefix">The prefix to filter the IDs by.</param>
    /// <param name="pageSize">
    /// The number of IDs to fetch per request. When left unspecified, the page size detemined by the server is used.
    /// As of the current version, the supported range is 1 to 100. Changing this value may affect throughput, memory and read units consumption.
    /// </param>
    /// <param name="readUnitsThreshold">The maximum number of read units to consume. The iterator will stop when the threshold is reached.</param>
    /// <param name="indexNamespace">Namespace to list vectors from. If no namespace is provided, the operation applies to all namespaces.</param> 
    public async IAsyncEnumerable<string> List(
        string? prefix = null,
        uint? pageSize = null,
        uint? readUnitsThreshold = null,
        string? indexNamespace = null,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var readUnits = 0u;
        var next = (string?)null;
        var threshold = readUnitsThreshold ?? uint.MaxValue;
        do 
        {
            (var ids, next, var units) = await ListPaginated(
                prefix,
                pageSize,
                next,
                indexNamespace,
                ct).ConfigureAwait(false);
            readUnits += units;
            foreach (var id in ids) yield return id;
        } while (next != null && readUnits < threshold);
    }

    /// <summary>
    /// Lists all vector IDs in the index filtered by the specified arguments by paginating through the entire index and collecting the results.
    /// <para/>
    /// This method is useful when performing data export or any similar case where materializing the contents of the index is necessary.
    /// Otherwise, you may want to use the either <see cref="List"/> or <see cref="ListPaginated"/> methods for more efficient listing.
    /// </summary>
    /// <param name="prefix">The prefix to filter the IDs by.</param>
    /// <param name="pageSize">
    /// The number of IDs to fetch per request. When left unspecified, the page size detemined by the server is used.
    /// As of the current version, the supported range is 1 to 100. Changing this value may affect throughput, memory and read units consumption.
    /// </param>
    /// <param name="paginationToken">The optional token to resume the listing from a specific point. See <see cref="ListOperationException.PaginationToken"/> for more information.</param>
    /// <param name="indexNamespace">Namespace to list vectors from. If no namespace is provided, the operation applies to all namespaces.</param>
    /// <exception cref="ListOperationException">
    /// Thrown when an error occurs during the listing operation. The exception contains the IDs of the vectors that were successfully listed,
    /// the pagination token that can be used to resume the listing, and the number of read units consumed.
    /// </exception>
    public async Task<(string[] VectorIds, uint ReadUnits)> ListAll(
        string? prefix = null,
        uint pageSize = 100,
        string? paginationToken = null,
        string? indexNamespace = null,
        CancellationToken ct = default)
    {
        var readUnits = 0u;
        var next = paginationToken;
        var pages = new List<string[]>();
        try
        {
            do
            {
                (var ids, next, var units) = await ListPaginated(
                    prefix,
                    pageSize,
                    next,
                    indexNamespace,
                    ct).ConfigureAwait(false);
                pages.Add(ids);
                readUnits += units;
            } while (next != null);
        }
        catch (Exception ex)
        {
            throw new ListOperationException(
                ex,
                pages.SelectMany(p => p).ToArray(),
                next,
                readUnits);
        }

        return (pages is [var single] ? single : pages.SelectMany(p => p).ToArray(), readUnits);
    }

    /// <summary>
    /// Lists vector IDs in the index using specified page size, prefix, and optional pagination token.
    /// </summary>
    /// <param name="prefix">The prefix to filter the IDs by.</param>
    /// <param name="pageSize">
    /// The number of IDs to fetch per request. When left unspecified, the page size detemined by the server is used.
    /// As of the current version, the supported range is 1 to 100. Changing this value may affect throughput, memory and read units consumption.
    /// </param>
    /// <param name="paginationToken">The pagination token to continue a previous listing operation.</param>
    /// <param name="indexNamespace">Namespace to list vectors from. If no namespace is provided, the operation applies to all namespaces.</param>
    public Task<(string[] VectorIds, string? PaginationToken, uint ReadUnits)> ListPaginated(
        string? prefix = null,
        uint? pageSize = null,
        string? paginationToken = null,
        string? indexNamespace = null,
        CancellationToken ct = default)
    {
        return Transport.List(prefix, pageSize, paginationToken, indexNamespace, ct);
    }

    /// <summary>
    /// Looks up and returns vectors by ID. The returned vectors include the vector data and/or metadata.
    /// </summary>
    /// <remarks>
    /// If the sequence of IDs is countable and greater than or equal to 600, it will be batched and the batches
    /// will be fetched in parallel. The default batch size is 200 and the default parallelism is 20.
    /// </remarks>
    /// <param name="ids">IDs of vectors to fetch.</param>
    /// <param name="indexNamespace">Namespace to fetch vectors from. If no namespace is provided, the operation applies to all namespaces.</param>
    /// <returns>A dictionary containing vector IDs and the corresponding <see cref="Vector"/> objects containing the vector information.</returns>
    /// <exception cref="ParallelFetchException">
    /// Thrown when the countable sequence of IDs is above the threshold batch size which initiates automatic parallel fetching
    /// and one or more batch fetch operations have failed.
    /// The exception contains the successfully fetched vectors and the inner batch fetch exceptions that occurred.
    /// This applies to .NET 6.0 or newer versions of this library.
    /// </exception>
    public Task<Dictionary<string, Vector>> Fetch(IEnumerable<string> ids, string? indexNamespace = null, CancellationToken ct = default)
    {
#if NET6_0_OR_GREATER
        var batchSize = GetBatchSize();

        if (ids.TryGetNonEnumeratedCount(out var count) && count > batchSize)
        {
            return Fetch(ids, batchSize, BatchParallelism, indexNamespace, ct);
        }
#endif
        return Transport.Fetch(ids, indexNamespace, ct);
    }

#if NET6_0_OR_GREATER
    /// <summary>
    /// Looks up and returns vectors by ID as batches in parallel.
    /// </summary>
    /// <param name="ids">IDs of vectors to fetch.</param>
    /// <param name="batchSize">The number of vectors to fetch in each batch.</param>
    /// <param name="parallelism">The maximum number of batches to process in parallel.</param>
    /// <param name="indexNamespace">Namespace to fetch vectors from. If no namespace is provided, the operation applies to all namespaces.</param>
    /// <returns>A dictionary containing vector IDs and the corresponding <see cref="Vector"/> objects containing the vector information.</returns>
    /// <exception cref="ParallelFetchException">
    /// Thrown when one or more batch fetch operations fail.
    /// The exception contains the successfully fetched vectors and the inner batch fetch exceptions that occurred.
    /// </exception>
    public async Task<Dictionary<string, Vector>> Fetch(
        IEnumerable<string> ids,
        int batchSize,
        int parallelism,
        string? indexNamespace = null,
        CancellationToken ct = default)
    {
        ThrowHelpers.CheckGreaterThan(batchSize, 0);
        ThrowHelpers.CheckGreaterThan(parallelism, 0);

        var fetched = new ConcurrentStack<Dictionary<string, Vector>>();
        var exceptions = (ConcurrentStack<Exception>?)null;
        var batches = ids.Chunk(batchSize);
        var options = new ParallelOptions
        {
            // We are intentionally not assigning the CancellationToken here.
            // See the Upsert method for more information.
            MaxDegreeOfParallelism = parallelism
        };

        Logger?.ParallelOperationStarted(batchSize, parallelism);
        await Parallel.ForEachAsync(batches, options, async (batch, _) =>
        {
            try
            {
                fetched.Push(await Transport.Fetch(batch, indexNamespace, ct));
            }
            catch (Exception ex)
            {
                if (exceptions is null) Interlocked.CompareExchange(ref exceptions, [], null);
                exceptions.Push(ex);
            }
        });

        var merged = new Dictionary<string, Vector>(fetched.SelectMany(batch => batch));
        if (exceptions != null)
        {
            var message = "One or more exceptions have occurred. " +
                $"Successfully fetched {merged.Count} vectors. Batches failed: {exceptions.Count}.";

            Logger?.ParallelOperationFailed(message);
            throw new ParallelFetchException(merged, message, [..exceptions]);
        }

        Logger?.ParallelOperationCompleted($"Fetched {merged.Count} vectors.");
        return merged;
    }
#endif

    /// <summary>
    /// Deletes vectors with specified ids.
    /// </summary>
    /// <param name="ids"></param>
    /// <param name="indexNamespace">Namespace to delete vectors from. If no namespace is provided, the operation applies to all namespaces.</param>
    public async Task Delete(IEnumerable<string> ids, string? indexNamespace = null, CancellationToken ct = default)
    {
#if NET6_0_OR_GREATER
        // Pinecone API limits delete batches to 1000 IDs each.
        // This presents an opportunity to address both the restriction and
        // improve the performance of the operation by sending batches in parallel.
        const int maxBatchSize = 1000;

        if (ids.TryGetNonEnumeratedCount(out var count) && count <= maxBatchSize)
        {
            await Transport.Delete(ids, indexNamespace, ct);
            return;
        }

        var deleted = 0u;
        var batches = ids.Chunk(maxBatchSize);
        var options = new ParallelOptions
        {
            // We are intentionally not assigning the CancellationToken here.
            // See the Upsert method for more information.
            MaxDegreeOfParallelism = BatchParallelism
        };

        var exceptions = (ConcurrentStack<Exception>?)null;
        var failedVectorBatches = (ConcurrentStack<string[]>?)null;

        Logger?.ParallelOperationStarted(maxBatchSize, BatchParallelism);
        await Parallel.ForEachAsync(batches, options, async (batch, _) =>
        {
            try
            {
                await Transport.Delete(batch, indexNamespace, ct);
                Interlocked.Add(ref deleted, (uint)batch.Length);
            }
            catch (Exception ex)
            {
                if (exceptions is null) Interlocked.CompareExchange(ref exceptions, [], null);
                exceptions.Push(ex);

                if (failedVectorBatches is null) Interlocked.CompareExchange(ref failedVectorBatches, [], null);
                failedVectorBatches.Push(batch);
            }
        });

        if (exceptions != null)
        {
            var message = "One or more exceptions have occurred. " +
                $"Successfully deleted {deleted} vectors. Batches failed: {exceptions.Count}.";

            Logger?.ParallelOperationFailed(message);
            throw new ParallelDeleteException(
                message,
                failedVectorBatches!.SelectMany(b => b).ToArray(),
                [..exceptions]);
        }

        Logger?.ParallelOperationCompleted($"Deleted {deleted} vectors.");
#else
        await Transport.Delete(ids, indexNamespace, ct);
#endif
    }

    /// <summary>
    /// Deletes vectors based on metadata filter provided.
    /// </summary>
    /// <param name="filter">Filter used to select vectors to delete.</param>
    /// <param name="indexNamespace">Namespace to delete vectors from. If no namespace is provided, the operation applies to all namespaces.</param>
    public Task Delete(MetadataMap filter, string? indexNamespace = null, CancellationToken ct = default)
    {
        return Transport.Delete(filter, indexNamespace, ct);
    }

    /// <summary>
    /// Deletes all vectors.
    /// </summary>
    /// <param name="indexNamespace">Namespace to delete vectors from. If no namespace is provided, the operation applies to all namespaces.</param>
    public Task DeleteAll(string? indexNamespace = null, CancellationToken ct = default)
    {
        return Transport.DeleteAll(indexNamespace, ct);
    }

    /// <inheritdoc />
    public void Dispose() => Transport.Dispose();

#if NET6_0_OR_GREATER
    private int GetBatchSize() => Dimension switch
    {
        // Targets < 2MiB of data per batch with some safety margin for metadata.
        <= 386 => 800,
        <= 768 => 400,
        <= 1536 => 200,
        <= 3072 => 100,
        <= 6144 => 50,
        <= 12288 => 25,
        _ => 10
    };
#endif
}

static partial class IndexLoggerExtensions
{
    [LoggerMessage(1, LogLevel.Information, "Parallel {operation} operation started. Batch size: {batchSize} Parallelism: {parallelism}")]
    public static partial void ParallelOperationStarted(
        this ILogger logger,
        int batchSize,
        int parallelism,
        [CallerMemberName] string operation = "");
    
    [LoggerMessage(2, LogLevel.Information, "Parallel {operation} operation completed. {outcome}")]
    public static partial void ParallelOperationCompleted(
        this ILogger logger,
        string outcome,
        [CallerMemberName] string operation = "");
    
    [LoggerMessage(3, LogLevel.Error, "Parallel {operation} operation failed. {message}")]
    public static partial void ParallelOperationFailed(
        this ILogger logger,
        string message,
        [CallerMemberName] string operation = "");
}
