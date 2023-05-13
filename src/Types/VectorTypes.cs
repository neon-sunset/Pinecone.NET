using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using Pinecone.Rest;

namespace Pinecone;

public record Vector
{
    public required string Id { get; init; }
    public required float[] Values { get; init; }
    public SparseVector? SparseValues { get; init; }
    public MetadataMap? Metadata { get; init; }
}

public readonly record struct SparseVector
{
    public required uint[] Indices { get; init; }
    public required float[] Values { get; init; }
}

public record ScoredVector
{
    public required string Id { get; init; }
    public required double Score { get; init; }
    public float[]? Values { get; init; }
    public SparseVector? SparseValues { get; init; }
    public MetadataMap? Metadata { get; init; }
}

public sealed class MetadataMap : Dictionary<string, MetadataValue> { }

[JsonConverter(typeof(MetadataValueConverter))]
public readonly record struct MetadataValue
{
    public object? Inner { get; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal MetadataValue(object? value) => Inner = value;

    public static implicit operator MetadataValue(bool value) => new(value);
    public static implicit operator MetadataValue(string? value) => new(value);
    public static implicit operator MetadataValue(int value) => new((double)value);
    public static implicit operator MetadataValue(long value) => new((double)value);
    public static implicit operator MetadataValue(float value) => new((double)value);
    public static implicit operator MetadataValue(double value) => new(value);
    public static implicit operator MetadataValue(decimal value) => new((double)value);
    public static implicit operator MetadataValue(MetadataMap value) => new(value);
    public static implicit operator MetadataValue(MetadataValue[]? value) => new(value);
    public static implicit operator MetadataValue(List<MetadataValue>? value) => new(value);
}
