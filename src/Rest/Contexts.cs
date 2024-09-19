using System.Text.Json.Serialization;

namespace Pinecone.Rest;

[JsonSerializable(typeof(string[]))]
[JsonSerializable(typeof(Metric))]
[JsonSerializable(typeof(IndexDetails))]
[JsonSerializable(typeof(MetadataValue[]))]
[JsonSerializable(typeof(CreateIndexRequest))]
[JsonSerializable(typeof(ConfigureIndexRequest))]
[JsonSerializable(typeof(CreateCollectionRequest))]
[JsonSerializable(typeof(CollectionDetails))]
[JsonSerializable(typeof(ListIndexesResult))]
[JsonSerializable(typeof(ListCollectionsResult))]
[JsonSourceGenerationOptions(
    GenerationMode = JsonSourceGenerationMode.Default,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
internal sealed partial class ClientContext : JsonSerializerContext;

[JsonSerializable(typeof(DescribeStatsRequest))]
[JsonSerializable(typeof(IndexStats))]
[JsonSerializable(typeof(QueryRequest))]
[JsonSerializable(typeof(QueryResponse))]
[JsonSerializable(typeof(UpsertRequest))]
[JsonSerializable(typeof(UpsertResponse))]
[JsonSerializable(typeof(UpdateRequest))]
[JsonSerializable(typeof(ListResponse))]
[JsonSerializable(typeof(FetchResponse))]
[JsonSerializable(typeof(DeleteRequest))]
[JsonSourceGenerationOptions(
    GenerationMode = JsonSourceGenerationMode.Default,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
internal sealed partial class RestTransportContext : JsonSerializerContext;