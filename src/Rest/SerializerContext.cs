using System.Text.Json.Serialization;
using Pinecone.Grpc;

namespace Pinecone.Rest;

[JsonSerializable(typeof(string[]))]
[JsonSerializable(typeof(PineconeMetric))]
[JsonSerializable(typeof(PineconeIndexStats))]
[JsonSerializable(typeof(PineconeIndexState))]
[JsonSerializable(typeof(PineconeIndexStatus))]
[JsonSerializable(typeof(PineconeIndexDetails))]
[JsonSerializable(typeof(PineconeIndex<GrpcTransport>))]
[JsonSerializable(typeof(PineconeIndex<RestTransport>))]
[JsonSerializable(typeof(CreateIndexRequest))]
[JsonSerializable(typeof(ConfigureIndexRequest))]
[JsonSerializable(typeof(MetadataMap))]
[JsonSerializable(typeof(MetadataValue))]
[JsonSerializable(typeof(MetadataValue[]))]
[JsonSourceGenerationOptions(
    GenerationMode = JsonSourceGenerationMode.Default,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
internal sealed partial class SerializerContext : JsonSerializerContext { }