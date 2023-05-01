using System.Globalization;
using CommunityToolkit.Diagnostics;
using Pinecone.Transport.Grpc;

namespace Pinecone;

public record PineconeVector
{
    public required string Id { get; init; }

    public required float[] Values { get; init; }

    public SparseValues? SparseValues { get; init; }

    public MetadataValue? Metadata { get; init; }
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

    public required SparseValues SparseValues { get; init; }

    public MetadataValue? Metadata { get; init; }
}

public readonly record struct MetadataValue
{
    private enum ValueKind { Null = 0, Number = 1, String = 2, Bool = 3, Struct = 4, List = 5 }

    public readonly object? Value;

    public MetadataValue(object? value)
    {
        Value = value;
        if (value switch
        {
            null or
            bool or
            string or
            int or uint or long or ulong or float or double or decimal or
            IDictionary<string, MetadataValue> or
            IEnumerable<MetadataValue> => true,
            _ => false
        }) { Value = value; }
        else
        {
            ThrowHelper.ThrowArgumentException($"Unsupported metadata value type: {value?.GetType()}");
        }
    }

    public static implicit operator Google.Protobuf.WellKnownTypes.Value(MetadataValue value) => value.Value switch
    {
        // This is terrible but such is life
        null => Google.Protobuf.WellKnownTypes.Value.ForNull(),
        int or uint or long or ulong or float or double or decimal => Google.Protobuf.WellKnownTypes.Value.ForNumber(
            Convert.ToDouble(value.Value, CultureInfo.InvariantCulture)),
        string strValue => Google.Protobuf.WellKnownTypes.Value.ForString(strValue),
        bool boolValue => Google.Protobuf.WellKnownTypes.Value.ForBool(boolValue),
        IDictionary<string, MetadataValue> structValue =>
            Google.Protobuf.WellKnownTypes.Value.ForStruct(structValue.ToProtoStruct()),
        IEnumerable<MetadataValue> listValue =>
            Google.Protobuf.WellKnownTypes.Value.ForList(listValue.Select(v => (Google.Protobuf.WellKnownTypes.Value)v).ToArray()),
        _ => ThrowHelper.ThrowArgumentException<Google.Protobuf.WellKnownTypes.Value>(
            $"Unsupported metadata value type: {value.Value!.GetType()}")
    };
}
