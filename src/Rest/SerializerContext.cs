using System.Text.Json.Serialization;

namespace Pinecone.Transport.Rest;

[JsonSerializable(typeof(string[]))]
[JsonSerializable(typeof(PineconeMetric))]
[JsonSerializable(typeof(PineconeIndexState))]
[JsonSerializable(typeof(PineconeIndexStatus))]
[JsonSerializable(typeof(PineconeIndexDetails))]
[JsonSerializable(typeof(PineconeIndex<RestTransport>))]
[JsonSerializable(typeof(PineconeIndex<GrpcTransport>))]
[JsonSerializable(typeof(CreateIndexRequest))]
[JsonSerializable(typeof(ConfigureIndexRequest))]
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
internal sealed partial class SerializerContext : JsonSerializerContext { }