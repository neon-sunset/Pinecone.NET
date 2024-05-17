using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

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
    public string? Host { get; init; }
    
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
/// <typeparam name="TTransport">The type of transport layer used.</typeparam>
public sealed partial record Index<TTransport> : IDisposable
    where TTransport : ITransport<TTransport>
{
    /// <summary>
    /// The transport layer.
    /// </summary>
    [JsonIgnore]
    internal TTransport Transport { get; set; } = default!;

    /// <summary>
    /// Returns statistics describing the contents of an index, including the vector count per namespace and the number of dimensions, and the index fullness.
    /// </summary>
    /// <param name="filter">The operation only returns statistics for vectors that satisfy the filter.</param>
    /// <returns>An <see cref="IndexStats"/> object containing index statistics.</returns>
    public Task<IndexStats> DescribeStats(MetadataMap? filter = null)
    {
        return Transport.DescribeStats(filter);
    }

    /// <summary>
    /// Searches an index using the values of a vector with specified ID. It retrieves the IDs of the most similar items, along with their similarity scores.
    /// </summary>
    /// <remarks>Query by ID uses Approximate Nearest Neighbor, which doesn't guarantee the input vector to appear in the results. To ensure that, use the Fetch operation instead.</remarks>
    /// <param name="id">The unique ID of the vector to be used as a query vector.</param>
    /// <param name="values">The query vector. This should be the same length as the dimension of the index being queried.</param>
    /// <param name="sparseValues">Vector sparse data. Represented as a list of indices and a list of corresponded values, which must be with the same length.</param>
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
        bool includeMetadata = false)
    {
        return Transport.Query(
            id: id,
            values: null,
            sparseValues: null,
            topK: topK,
            filter: filter,
            indexNamespace: indexNamespace,
            includeValues: includeValues,
            includeMetadata: includeMetadata);
    }

    /// <summary>
    /// Searches an index using the specified vector values. It retrieves the IDs of the most similar items, along with their similarity scores.
    /// </summary>
    /// <param name="values">The query vector. This should be the same length as the dimension of the index being queried.</param>
    /// <param name="sparseValues">Vector sparse data. Represented as a list of indices and a list of corresponded values, which must be with the same length.</param>
    /// <param name="topK">The number of results to return for each query.</param>
    /// <param name="filter">The filter to apply.</param>
    /// <param name="indexNamespace">Namespace to query from. If no namespace is provided, the operation applies to all namespaces.</param>
    /// <param name="includeValues">Indicates whether vector values are included in the response.</param>
    /// <param name="includeMetadata">Indicates whether metadata is included in the response as well as the IDs.</param>
    /// <returns></returns>
    public Task<ScoredVector[]> Query(
        float[] values,
        uint topK,
        MetadataMap? filter = null,
        SparseVector? sparseValues = null,
        string? indexNamespace = null,
        bool includeValues = true,
        bool includeMetadata = false)
    {
        return Transport.Query(
            id: null,
            values: values,
            sparseValues: sparseValues,
            topK: topK,
            filter: filter,
            indexNamespace: indexNamespace,
            includeValues: includeValues,
            includeMetadata: includeMetadata);
    }

    /// <summary>
    /// Writes vector into the index. If a new value is provided for an existing vector ID, it will overwrite the previous value.
    /// </summary>
    /// <param name="vectors">A collection of <see cref="Vector"/> objects to upsert.</param>
    /// <param name="indexNamespace">Namespace to write the vector to. If no namespace is provided, the operation applies to all namespaces.</param>
    /// <returns>The number of vectors upserted.</returns>
    public Task<uint> Upsert(IEnumerable<Vector> vectors, string? indexNamespace = null)
    {
        return Transport.Upsert(vectors, indexNamespace);
    }

    /// <summary>
    /// Updates a vector using the <see cref="Vector"/> object.
    /// </summary>
    /// <param name="vector"><see cref="Vector"/> object containing updated information.</param>
    /// <param name="indexNamespace">Namespace to update the vector from. If no namespace is provided, the operation applies to all namespaces.</param>
    public Task Update(Vector vector, string? indexNamespace = null)
    {
        return Transport.Update(vector, indexNamespace);
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
        float[]? values = null,
        SparseVector? sparseValues = null,
        MetadataMap? metadata = null,
        string? indexNamespace = null)
    {
        return Transport.Update(id, values, sparseValues, metadata, indexNamespace);
    }

    /// <summary>
    /// Looks up and returns vectors, by ID. The returned vectors include the vector data and/or metadata.
    /// </summary>
    /// <param name="ids">IDs of vectors to fetch.</param>
    /// <param name="indexNamespace">Namespace to fetch vectors from. If no namespace is provided, the operation applies to all namespaces.</param>
    /// <returns>A dictionary containing vector IDs and the corresponding <see cref="Vector"/> objects containing the vector information.</returns>
    public Task<Dictionary<string, Vector>> Fetch(IEnumerable<string> ids, string? indexNamespace = null)
    {
        return Transport.Fetch(ids, indexNamespace);
    }

    /// <summary>
    /// Deletes vectors with specified ids.
    /// </summary>
    /// <param name="ids"></param>
    /// <param name="indexNamespace">Namespace to delete vectors from. If no namespace is provided, the operation applies to all namespaces.</param>
    public Task Delete(IEnumerable<string> ids, string? indexNamespace = null)
    {
        return Transport.Delete(ids, indexNamespace);
    }

    /// <summary>
    /// Deletes vectors based on metadata filter provided.
    /// </summary>
    /// <param name="filter">Filter used to select vectors to delete.</param>
    /// <param name="indexNamespace">Namespace to delete vectors from. If no namespace is provided, the operation applies to all namespaces.</param>
    public Task Delete(MetadataMap filter, string? indexNamespace = null)
    {
        return Transport.Delete(filter, indexNamespace);
    }

    /// <summary>
    /// Deletes all vectors.
    /// </summary>
    /// <param name="indexNamespace">Namespace to delete vectors from. If no namespace is provided, the operation applies to all namespaces.</param>
    public Task DeleteAll(string? indexNamespace = null)
    {
        return Transport.DeleteAll(indexNamespace);
    }

    /// <inheritdoc />
    public void Dispose() => Transport.Dispose();
}
