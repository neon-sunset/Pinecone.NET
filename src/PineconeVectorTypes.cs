using System.Text.Json.Serialization;
using CommunityToolkit.Diagnostics;
using Pinecone.Rest;

namespace Pinecone;

public record PineconeVector
{
    public required string Id { get; init; }

    public required float[] Values { get; init; }

    public SparseValues? SparseValues { get; init; }

    public MetadataMap? Metadata { get; init; }
}

public readonly record struct SparseValues
{
    public required uint[] Indices { get; init; }

    public required float[] Values { get; init; }
}

public record ScoredVector
{
    public required string Id { get; init; }

    public required float Score { get; init; }

    public required float[] Values { get; init; }

    public SparseValues? SparseValues { get; init; }

    public MetadataMap? Metadata { get; init; }
}

public sealed class MetadataMap : Dictionary<string, MetadataValue> { }

[JsonConverter(typeof(MetadataValueConverter))]
public readonly record struct MetadataValue
{
    public object? Inner { get; }

    public MetadataValue(object? value)
    {
        Inner = value;
        if (value switch
        {
            null or
            bool or
            string or
            int or uint or long or ulong or float or double or decimal or
            MetadataMap or
            IEnumerable<MetadataValue> => true,
            _ => false
        }) { Inner = value; }
        else
        {
            ThrowHelper.ThrowArgumentException($"Unsupported metadata type: {value!.GetType()}");
        }
    }

    public static implicit operator MetadataValue(bool value) => new(value);
    public static implicit operator MetadataValue(string value) => new(value);
    public static implicit operator MetadataValue(int value) => new(value);
    public static implicit operator MetadataValue(float value) => new(value);
    public static implicit operator MetadataValue(double value) => new(value);
    public static implicit operator MetadataValue(decimal value) => new(value);
    public static implicit operator MetadataValue(MetadataMap value) => new(value);
    public static implicit operator MetadataValue(MetadataValue[] value) => new(value);
    public static implicit operator MetadataValue(List<MetadataValue> value) => new(value);
}
