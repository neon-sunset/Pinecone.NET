using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
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

    public static Value ToProtoValue(this MetadataValue source) => source.Inner switch
    {
        // This is terrible but such is life
        null => Value.ForNull(),
        double num => Value.ForNumber(num),
        string str => Value.ForString(str),
        bool boolean => Value.ForBool(boolean),
        MetadataMap nested => Value.ForStruct(nested.ToProtoStruct()),
        IEnumerable<MetadataValue> list => Value.ForList(list.Select(v => v.ToProtoValue()).ToArray()),
        _ => ThrowHelpers.ArgumentException<Value>($"Unsupported metadata type: {source.Inner!.GetType()}")
    };

    public static Vector ToProtoVector(this Pinecone.Vector source)
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

    public static Pinecone.Vector ToPublicType(this Vector source) => new()
    {
        Id = source.Id,
        Values = source.Values.AsMemory(),
        SparseValues = source.SparseValues?.Indices.Count > 0
            ? new SparseVector
            {
                Indices = source.SparseValues.Indices.AsMemory(),
                Values = source.SparseValues.Values.AsMemory()
            }
            : null,
        Metadata = source.Metadata?.Fields.ToPublicType()
    };

    public static Pinecone.ScoredVector ToPublicType(this ScoredVector source) => new()
    {
        Id = source.Id,
        Score = source.Score,
        Values = source.Values.AsMemory(),
        SparseValues = source.SparseValues?.Indices.Count > 0 ? new()
        {
            Indices = source.SparseValues.Indices.AsMemory(),
            Values = source.SparseValues.Values.AsMemory()
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
            _ => ThrowHelpers.ArgumentException<MetadataValue>($"Unsupported metadata type: {source.KindCase}")
        };
    }
    
#if NET8_0_OR_GREATER
    // These have to be duplicated because unsafe accessor does not support generics in .NET 8.
    // This approach is, however, very useful as we completely bypass referencing reflection for NAOT.
    public static ReadOnlyMemory<float> AsMemory(this RepeatedField<float> source)
    {
        return ArrayRef(source).AsMemory(0, source.Count);
    }

    public static void OverwriteWith(this RepeatedField<float> target, ReadOnlyMemory<float>? source)
    {
        if (source is null or { IsEmpty: true }) return;

        float[] array;
        int count;
        if (MemoryMarshal.TryGetArray(source.Value, out var segment)
            && segment.Offset is 0)
        {
            array = segment.Array!;
            count = segment.Count;
        }
        else
        {
            array = source.Value.ToArray();
            count = array.Length;
        }

        ArrayRef(target) = array;
        CountRef(target) = count;
    }

    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "array")]
    static extern ref float[] ArrayRef(RepeatedField<float> instance);

    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "count")]
    static extern ref int CountRef(RepeatedField<float> instance);

    public static ReadOnlyMemory<uint> AsMemory(this RepeatedField<uint> source)
    {
        return ArrayRef(source).AsMemory(0, source.Count);
    }

    public static void OverwriteWith(this RepeatedField<uint> target, ReadOnlyMemory<uint>? source)
    {
        if (source is null or { IsEmpty: true }) return;

        uint[] array;
        int count;
        if (MemoryMarshal.TryGetArray(source.Value, out var segment)
            && segment.Offset is 0)
        {
            array = segment.Array!;
            count = segment.Count;
        }
        else
        {
            array = source.Value.ToArray();
            count = array.Length;
        }

        ArrayRef(target) = array;
        CountRef(target) = count;
    }

    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "array")]
    static extern ref uint[] ArrayRef(RepeatedField<uint> instance);
    
    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "count")]
    static extern ref int CountRef(RepeatedField<uint> instance);
#else
    public static ReadOnlyMemory<T> AsMemory<T>(this RepeatedField<T> source)
        where T : unmanaged
    {
        return FieldAccessors<T>.GetArray(source).AsMemory(0, source.Count);
    }

    public static void OverwriteWith<T>(this RepeatedField<T> target, ReadOnlyMemory<T>? source)
        where T : unmanaged
    {
        if (source is null or { IsEmpty: true }) return;

        T[] array;
        int count;
        if (MemoryMarshal.TryGetArray(source.Value, out var segment)
            && segment.Offset is 0)
        {
            array = segment.Array!;
            count = segment.Count;
        }
        else
        {
            array = source.Value.ToArray();
            count = array.Length;
        }

        FieldAccessors<T>.SetArray(target, array);
        FieldAccessors<T>.SetCount(target, count);
    }

    private static class FieldAccessors<T> where T : unmanaged
    {
        public static T[] GetArray(RepeatedField<T> instance)
        {
            return (T[])ArrayField.GetValue(instance)!;
        }

        public static void SetArray(RepeatedField<T> instance, T[] value)
        {
            ArrayField.SetValue(instance, value);
        }

        public static void SetCount(RepeatedField<T> instance, int value)
        {
            CountField.SetValue(instance, value);
        }

        static readonly FieldInfo ArrayField = typeof(RepeatedField<T>)
            .GetField("array", BindingFlags.NonPublic | BindingFlags.Instance) ?? throw new NullReferenceException();

        static readonly FieldInfo CountField = typeof(RepeatedField<T>)
            .GetField("count", BindingFlags.NonPublic | BindingFlags.Instance) ?? throw new NullReferenceException();
    }
#endif
}
