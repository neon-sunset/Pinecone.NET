using System.Reflection;
using Google.Protobuf.Collections;
using Google.Protobuf.WellKnownTypes;

namespace Pinecone.Transport.Grpc;

internal static class Extensions
{
    private static class FieldAccessors<T> where T : unmanaged
    {
        public static readonly FieldInfo ArrayField = typeof(RepeatedField<T>)
            .GetField("array", BindingFlags.NonPublic | BindingFlags.Instance) ?? throw new NullReferenceException();

        public static readonly FieldInfo CountField = typeof(RepeatedField<T>)
            .GetField("count", BindingFlags.NonPublic | BindingFlags.Instance) ?? throw new NullReferenceException();
    }

    // gRPC types conversion to sane and usable ones
    public static Struct ToProtoStruct(this IEnumerable<KeyValuePair<string, MetadataValue>> source)
    {
        var protoStruct = new Struct();
        foreach (var (key, value) in source)
        {
            protoStruct.Fields.Add(key, value);
        }

        return protoStruct;
    }

    public static Struct? ToProtoStruct(this MetadataValue source) => source.Value switch
    {
        null => null,
        IDictionary<string, MetadataValue> structValue => structValue.ToProtoStruct(),
        _ => new Struct
        {
            Fields = { { "value", source } }
        }
    };

    public static Vector ToProtoVector(this PineconeVector source)
    {
        var protoVector = new Vector
        {
            Id = source.Id,
            SparseValues = source.SparseValues?.ToProtoSparseValues(),
            Metadata = source.Metadata?.ToProtoStruct()
        };
        protoVector.Values.OverwriteWith(source.Values);

        return protoVector;
    }

    public static global::SparseValues ToProtoSparseValues(this SparseValues source)
    {
        var protoSparseValues = new global::SparseValues();
        protoSparseValues.Indices.OverwriteWith(source.Indices);
        protoSparseValues.Values.OverwriteWith(source.Values);

        return protoSparseValues;
    }

    public static PineconeIndexStats ToPublicType(this DescribeIndexStatsResponse source) => new()
    {
        Namespaces = source.Namespaces
            .Select(kvp => new PineconeIndexNamespace
            {
                Name = kvp.Key,
                VectorCount = kvp.Value.VectorCount
            })
            .ToArray(),
        Dimension = source.Dimension,
        IndexFullness = source.IndexFullness,
        TotalVectorCount = source.TotalVectorCount
    };

    public static PineconeVector ToPublicType(this Vector source)
    {
        return new PineconeVector
        {
            Id = source.Id,
            Values = source.Values.AsArray(),
            SparseValues = source.SparseValues?.Indices.Count > 0
                ? new SparseValues
                {
                    Indices = source.SparseValues.Indices.AsArray(),
                    Values = source.SparseValues.Values.AsArray()
                }
                : null,
            // Metadata = source.Metadata?.Fields.ToDictionary(
            //     kvp => kvp.Key,
            //     kvp => kvp.Value.KindCase)
        };
    }

    public static ScoredVector ToPublicType(this global::ScoredVector source) => new()
    {
        Id = source.Id,
        Score = source.Score,
        Values = source.Values.AsArray(),
        SparseValues = new()
        {
            Indices = source.SparseValues.Indices.AsArray(),
            Values = source.SparseValues.Values.AsArray()
        }
    };

    // TODO: Refactor how MetadataValue is exposed to the user, ensure correct global::Struct conversion (both ways)
    public static MetadataValue? ToPublicType(this Struct source) => source.Fields?.Count > 0
        ? new MetadataValue(source.Fields.ToDictionary(
            kvp => kvp.Key,
            kvp => (object?)(kvp.Value.KindCase switch
            {
                Value.KindOneofCase.BoolValue => kvp.Value.BoolValue,
                Value.KindOneofCase.NumberValue => kvp.Value.NumberValue,
                Value.KindOneofCase.StringValue => kvp.Value.StringValue,
                Value.KindOneofCase.StructValue => kvp.Value.StructValue,
                // Value.KindOneofCase.ListValue => kvp.Value.ListValue.Values.Select(v => v.ToPublicType()).ToArray(),
                _ => null
            })))
        : null;

    public static RepeatedField<T> AsRepeatedField<T>(this T[] source) where T : unmanaged
    {
        var repeatedField = new RepeatedField<T>();
        FieldAccessors<T>.ArrayField.SetValue(repeatedField, source);
        FieldAccessors<T>.CountField.SetValue(repeatedField, source.Length);

        return repeatedField;
    }

    public static T[] AsArray<T>(this RepeatedField<T> source) where T : unmanaged
    {
        return (T[])FieldAccessors<T>.ArrayField.GetValue(source)!;
    }

    public static void OverwriteWith<T>(this RepeatedField<T> target, T[] source) where T : unmanaged
    {
        FieldAccessors<T>.ArrayField.SetValue(target, source);
        FieldAccessors<T>.CountField.SetValue(target, source.Length);
    }
}
