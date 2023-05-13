using System.Text.Json.Serialization;
using Pinecone.Grpc;

namespace Pinecone.Rest;

[JsonSerializable(typeof(string[]))]
[JsonSerializable(typeof(Metric))]
[JsonSerializable(typeof(IndexStats))]
[JsonSerializable(typeof(IndexState))]
[JsonSerializable(typeof(IndexStatus))]
[JsonSerializable(typeof(IndexDetails))]
[JsonSerializable(typeof(Index<GrpcTransport>))]
[JsonSerializable(typeof(Index<RestTransport>))]
[JsonSerializable(typeof(CreateIndexRequest))]
[JsonSerializable(typeof(ConfigureIndexRequest))]
[JsonSerializable(typeof(DescribeStatsRequest))]
[JsonSerializable(typeof(CreateCollectionRequest))]
[JsonSerializable(typeof(CollectionDetails))]
[JsonSerializable(typeof(QueryRequest))]
[JsonSerializable(typeof(QueryResponse))]
[JsonSerializable(typeof(UpsertRequest))]
[JsonSerializable(typeof(UpsertResponse))]
[JsonSerializable(typeof(UpdateRequest))]
[JsonSerializable(typeof(FetchResponse))]
[JsonSerializable(typeof(DeleteRequest))]
[JsonSerializable(typeof(MetadataMap))]
[JsonSerializable(typeof(MetadataValue[]))]
[JsonSourceGenerationOptions(
    GenerationMode = JsonSourceGenerationMode.Default,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
internal sealed partial class SerializerContext : JsonSerializerContext { }