using System.Reflection;
using Google.Protobuf.Collections;
using Google.Protobuf.WellKnownTypes;

namespace Pinecone.Transport.Grpc;

internal static class Extensions
{
    static class FieldAccessors<T> where T : unmanaged
    {
        public static readonly FieldInfo ArrayField = typeof(RepeatedField<T>)
            .GetField("array", BindingFlags.NonPublic | BindingFlags.Instance) ?? throw new NullReferenceException();

        public static readonly FieldInfo CountField = typeof(RepeatedField<T>)
            .GetField("count", BindingFlags.NonPublic | BindingFlags.Instance) ?? throw new NullReferenceException();
    }

    // gRPC types conversion to sane and usable ones
    public static Struct ToProtoStruct(this IEnumerable<KeyValuePair<string, string>> source)
    {
        var protoStruct = new Struct();
        foreach (var (key, value) in source)
        {
            protoStruct.Fields.Add(key, new Value { StringValue = value });
        }

        return protoStruct;
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
