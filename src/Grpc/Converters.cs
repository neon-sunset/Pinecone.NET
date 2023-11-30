using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using CommunityToolkit.Diagnostics;
using Google.Protobuf.Collections;
using Google.Protobuf.WellKnownTypes;

namespace Pinecone.Grpc;

internal static class Converters
{
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
            double num => Value.ForNumber(num),
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
            }).ToArray() : [],
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

    public static T[] AsArray<T>(this RepeatedField<T> source) where T : unmanaged
    {
        var buffer = FieldAccessors<T>.GetArray(source);
        if (buffer.Length != source.Count)
        {
            buffer = buffer[..source.Count];
        }

        return buffer;
    }

    public static void OverwriteWith<T>(this RepeatedField<T> target, T[]? source) where T : unmanaged
    {
        if (source is null) return;

        FieldAccessors<T>.SetArray(target, source);
        FieldAccessors<T>.SetCount(target, source.Length);
    }

    private static class FieldAccessors<T> where T : unmanaged
    {
        public static T[] GetArray(RepeatedField<T> instance)
        {
#if NET8_0_OR_GREATER
            if (instance is RepeatedField<float> floatSeq)
            {
                return (T[])(object)ArrayRef(floatSeq);
            }
#endif

            return (T[])ArrayField.GetValue(instance)!;
        }

        public static void SetArray(RepeatedField<T> instance, T[] value)
        {
#if NET8_0_OR_GREATER
            if (instance is RepeatedField<float> floatSeq)
            {
                ArrayRef(floatSeq) = (float[])(object)value;
                return;
            }
#endif

            ArrayField.SetValue(instance, value);
        }

        public static void SetCount(RepeatedField<T> instance, int value)
        {
#if NET8_0_OR_GREATER
            if (instance is RepeatedField<float> floatSeq)
            {
                CountRef(floatSeq) = value;
                return;
            }
#endif

            CountField.SetValue(instance, value);
        }

        static readonly FieldInfo ArrayField = typeof(RepeatedField<T>)
            .GetField("array", BindingFlags.NonPublic | BindingFlags.Instance) ?? throw new NullReferenceException();

        static readonly FieldInfo CountField = typeof(RepeatedField<T>)
            .GetField("count", BindingFlags.NonPublic | BindingFlags.Instance) ?? throw new NullReferenceException();
    }

#if NET8_0_OR_GREATER
    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "array")]
    static extern ref float[] ArrayRef(RepeatedField<float> instance);

    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "count")]
    static extern ref int CountRef(RepeatedField<float> instance);
#endif
}
