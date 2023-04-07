using System.Text.Json.Serialization;

namespace Pinecone.Serialization;

[JsonSerializable(typeof(string[]))]
[JsonSerializable(typeof(PineconeMetric))]
[JsonSerializable(typeof(PineconeIndex))]
[JsonSerializable(typeof(PineconeIndexStatus))]
[JsonSerializable(typeof(PineconeIndexDetails))]
[JsonSerializable(typeof(CreateIndexRequest))]
[JsonSerializable(typeof(ConfigureIndexRequest))]
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
internal sealed partial class SerializerContext : JsonSerializerContext { }