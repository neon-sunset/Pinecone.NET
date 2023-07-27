using System.Globalization;
using System.Reflection;
using CommunityToolkit.Diagnostics;
using Google.Protobuf.Collections;
using Google.Protobuf.WellKnownTypes;

namespace Pinecone.Grpc;

internal static class Converters
{
    private static class FieldAccessors<T> where T : unmanaged
    {
        public static readonly FieldInfo ArrayField = typeof(RepeatedField<T>)
            .GetField("array", BindingFlags.NonPublic | BindingFlags.Instance) ?? throw new NullReferenceException();

        public static readonly FieldInfo CountField = typeof(RepeatedField<T>)
            .GetField("count", BindingFlags.NonPublic | BindingFlags.Instance) ?? throw new NullReferenceException();
    }

    // gRPC types conversion to sane and usable ones
    public static Struct ToProtoStruct(this MetadataMap source)
    {
        var protoStruct = new Struct();
        foreach (var (key, value) in source)
        {
            protoStruct.Fields.Add(key, value.ToProtoValue());
        }

        return protoStruct;
    }

    public static Value ToProtoValue(this MetadataValue source)
    {
        return source.Inner switch
        {
            // This is terrible but such is life
            null => Value.ForNull(),
            int or uint or long or ulong or float or double or decimal =>
                Value.ForNumber(Convert.ToDouble(source.Inner, CultureInfo.InvariantCulture)),
            string str => Value.ForString(str),
            bool boolean => Value.ForBool(boolean),
            MetadataMap nested => Value.ForStruct(nested.ToProtoStruct()),
            IEnumerable<MetadataValue> list => Value.ForList(list.Select(v => v.ToProtoValue()).ToArray()),
            _ => ThrowHelper.ThrowArgumentException<Value>($"Unsupported metadata type: {source.Inner!.GetType()}")
        };
    }

    public static global::Vector ToProtoVector(this Vector source)
    {
        var protoVector = new global::Vector
        {
            Id = source.Id,
            SparseValues = source.SparseValues?.ToProtoSparseValues(),
            Metadata = source.Metadata?.ToProtoStruct()
        };
        protoVector.Values.OverwriteWith(source.Values);

        return protoVector;
    }

    public static SparseValues ToProtoSparseValues(this SparseVector source)
    {
        var protoSparseValues = new SparseValues();
        protoSparseValues.Indices.OverwriteWith(source.Indices);
        protoSparseValues.Values.OverwriteWith(source.Values);

        return protoSparseValues;
    }

    public static IndexStats ToPublicType(this DescribeIndexStatsResponse source) => new()
    {
        Namespaces = source.Namespaces.Count > 0 ?
            source.Namespaces.Select(kvp => new IndexNamespace
            {
                Name = kvp.Key,
                VectorCount = kvp.Value.VectorCount
            }).ToArray() : Array.Empty<IndexNamespace>(),
        Dimension = source.Dimension,
        IndexFullness = source.IndexFullness,
        TotalVectorCount = source.TotalVectorCount
    };

    public static Vector ToPublicType(this global::Vector source)
    {
        return new Vector
        {
            Id = source.Id,
            Values = source.Values.AsArray(),
            SparseValues = source.SparseValues?.Indices.Count > 0
                ? new SparseVector
                {
                    Indices = source.SparseValues.Indices.AsArray(),
                    Values = source.SparseValues.Values.AsArray()
                }
                : null,
            Metadata = source.Metadata?.Fields.ToPublicType()
        };
    }

    public static ScoredVector ToPublicType(this global::ScoredVector source) => new()
    {
        Id = source.Id,
        Score = source.Score,
        Values = source.Values.AsArray(),
        SparseValues = source.SparseValues?.Indices.Count > 0 ? new()
        {
            Indices = source.SparseValues.Indices.AsArray(),
            Values = source.SparseValues.Values.AsArray()
        } : null,
        Metadata = source.Metadata?.Fields.ToPublicType()
    };

    public static MetadataMap ToPublicType(this MapField<string, Value> source)
    {
        var metadata = new MetadataMap();
        foreach (var (key, value) in source)
        {
            metadata.Add(key, value.ToPublicType());
        }
        return metadata;
    }

    public static MetadataValue ToPublicType(this Value source)
    {
        return source.KindCase switch
        {
            Value.KindOneofCase.None or
            Value.KindOneofCase.NullValue => new(),
            Value.KindOneofCase.NumberValue => new(source.NumberValue),
            Value.KindOneofCase.StringValue => new(source.StringValue),
            Value.KindOneofCase.BoolValue => new(source.BoolValue),
            Value.KindOneofCase.StructValue => new(source.StructValue.Fields.ToPublicType()),
            Value.KindOneofCase.ListValue => new(source.ListValue.Values.Select(v => v.ToPublicType()).ToArray()),
            _ => ThrowHelper.ThrowArgumentException<MetadataValue>($"Unsupported metadata type: {source.KindCase}")
        };
    }

    public static RepeatedField<T> AsRepeatedField<T>(this T[] source) where T : unmanaged
    {
        var repeatedField = new RepeatedField<T>();
        FieldAccessors<T>.ArrayField.SetValue(repeatedField, source);
        FieldAccessors<T>.CountField.SetValue(repeatedField, source.Length);

        return repeatedField;
    }

    public static T[] AsArray<T>(this RepeatedField<T> source) where T : unmanaged
    {
        var buffer = (T[])FieldAccessors<T>.ArrayField.GetValue(source)!;
        if (buffer.Length != source.Count)
        {
            buffer = buffer[..source.Count];
        }

        return buffer;
    }

    public static void OverwriteWith<T>(this RepeatedField<T> target, T[]? source) where T : unmanaged
    {
        if (source is null) return;

        FieldAccessors<T>.ArrayField.SetValue(target, source);
        FieldAccessors<T>.CountField.SetValue(target, source.Length);
    }
}
