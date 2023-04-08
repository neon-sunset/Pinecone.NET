using Google.Protobuf.WellKnownTypes;

namespace Pinecone.Transport.Grpc;

internal static class Extensions
{
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
}
