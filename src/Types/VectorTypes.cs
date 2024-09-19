using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using Pinecone.Rest;

namespace Pinecone;

/// <summary>
/// An object representing a vector.
/// </summary>
public record Vector
{
    /// <summary>
    /// Unique ID of the vector.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Vector data.
    /// </summary>
    public required ReadOnlyMemory<float> Values { get; init; }

    /// <summary>
    /// Sparse vector information.
    /// </summary>
    public SparseVector? SparseValues { get; init; }

    /// <summary>
    /// Metadata associated with this vector.
    /// </summary>
    public MetadataMap? Metadata { get; init; }
}

/// <summary>
/// Contains sparse vector information.
/// </summary>
public readonly record struct SparseVector
{
    /// <summary>
    /// The indices of the sparse data.
    /// </summary>
    public required ReadOnlyMemory<uint> Indices { get; init; }

    /// <summary>
    /// The corresponding values of the sparse data, which must be with the same length as the indices.
    /// </summary>
    public required ReadOnlyMemory<float> Values { get; init; }
}

/// <summary>
/// Vector returned as a result of a query operation. Contains regular vector information as well as similarity score.
/// </summary>
public record ScoredVector
{
    /// <summary>
    /// Unique ID of the vector.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// This is a measure of similarity between this vector and the query vector. The higher the score, the more they are similar.
    /// </summary>
    public required double Score { get; init; }

    /// <summary>
    /// Vector data.
    /// </summary>
    public ReadOnlyMemory<float>? Values { get; init; }

    /// <summary>
    /// Sparse vector information.
    /// </summary>
    public SparseVector? SparseValues { get; init; }

    /// <summary>
    /// Metadata associated with this vector.
    /// </summary>
    public MetadataMap? Metadata { get; init; }
}

/// <summary>
/// Collection of metadata consisting of key-value-pairs of property names and their corresponding values.
/// </summary>
public sealed class MetadataMap : Dictionary<string, MetadataValue> 
{
    /// <summary>
    /// Creates a new instance of the <see cref="MetadataMap" /> class.
    /// </summary>
    public MetadataMap() : base() { }

    /// <summary>
    /// Creates a new instance of the <see cref="MetadataMap" /> class from an existing collection.
    /// </summary>
    /// <param name="collection"></param>
    public MetadataMap(IEnumerable<KeyValuePair<string, MetadataValue>> collection)
#if NETSTANDARD2_0
        : base(collection.ToDictionary(e => e.Key, e => e.Value))
#else
        : base(collection)
#endif
    { }
}

/// <summary>
/// Value corresponding to a metadata property.
/// </summary>
[JsonConverter(typeof(MetadataValueConverter))]
public readonly record struct MetadataValue
{
    /// <summary>
    /// Metadata value stored.
    /// </summary>
    public object? Inner { get; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal MetadataValue(object? value) => Inner = value;

    /// <summary>
    /// Tries to create a new instance of the <see cref="MetadataValue" /> from the specified value.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="value">The value to create a metadata value from.</param>
    /// <param name="metadataValue">The metadata value created.</param>
    /// <returns><c>true</c> if the metadata value supports provided type of value; otherwise, <c>false</c>.</returns>
    public static bool TryCreate<T>(T? value, out MetadataValue metadataValue)
    {
        switch (value)
        {
            case null: metadataValue = default; return false;
            case bool b: metadataValue = b; return true;
            case string s: metadataValue = s; return true;
            case int i: metadataValue = i; return true;
            case long l: metadataValue = l; return true;
            case float f: metadataValue = f; return true;
            case double d: metadataValue = d; return true;
            case decimal m: metadataValue = m; return true;
            case MetadataMap map: metadataValue = map; return true;
            case MetadataValue[] array: metadataValue = array; return true;
            case List<MetadataValue> list: metadataValue = list; return true;

            case string[] s: metadataValue = s; return true;
            case int[] i: metadataValue = i; return true;
            case long[] l: metadataValue = l; return true;
            case float[] f: metadataValue = f; return true;
            case double[] d: metadataValue = d; return true;
            case decimal[] m: metadataValue = m; return true;

            case List<string> s: metadataValue = s; return true;
            case List<int> i: metadataValue = i; return true;
            case List<long> l: metadataValue = l; return true;
            case List<float> f: metadataValue = f; return true;
            case List<double> d: metadataValue = d; return true;
            case List<decimal> m: metadataValue = m; return true;

            default: metadataValue = default; return false;
        }
    }

    public override string ToString() => Inner?.ToString() ?? "null";

    // Main supported types
    public static implicit operator MetadataValue(bool value) => new(value);
    public static implicit operator MetadataValue(string? value) => new(value);
    public static implicit operator MetadataValue(int value) => new((double)value);
    public static implicit operator MetadataValue(long value) => new((double)value);
    public static implicit operator MetadataValue(float value) => new((double)value);
    public static implicit operator MetadataValue(double value) => new(value);
    public static implicit operator MetadataValue(decimal value) => new((double)value);
    public static implicit operator MetadataValue(MetadataMap? value) => new(value);
    public static implicit operator MetadataValue(MetadataValue[]? value) => new(value);
    public static implicit operator MetadataValue(List<MetadataValue>? value) => new(value);

    // Compatible conversions: arrays
    public static implicit operator MetadataValue(string[]? value) => new(value?.Select(v => (MetadataValue)v));
    public static implicit operator MetadataValue(int[]? value) => new(value?.Select(v => (MetadataValue)v));
    public static implicit operator MetadataValue(long[]? value) => new(value?.Select(v => (MetadataValue)v));
    public static implicit operator MetadataValue(float[]? value) => new(value?.Select(v => (MetadataValue)v));
    public static implicit operator MetadataValue(double[]? value) => new(value?.Select(v => (MetadataValue)v));
    public static implicit operator MetadataValue(decimal[]? value) => new(value?.Select(v => (MetadataValue)v));

    // Compatible conversions: lists
    public static implicit operator MetadataValue(List<string>? value) => new(value?.Select(v => (MetadataValue)v));
    public static implicit operator MetadataValue(List<int>? value) => new(value?.Select(v => (MetadataValue)v));
    public static implicit operator MetadataValue(List<long>? value) => new(value?.Select(v => (MetadataValue)v));
    public static implicit operator MetadataValue(List<float>? value) => new(value?.Select(v => (MetadataValue)v));
    public static implicit operator MetadataValue(List<double>? value) => new(value?.Select(v => (MetadataValue)v));
    public static implicit operator MetadataValue(List<decimal>? value) => new(value?.Select(v => (MetadataValue)v));
}
