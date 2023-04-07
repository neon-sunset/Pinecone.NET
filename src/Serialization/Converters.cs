using System.Runtime.Serialization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Pinecone.Serialization;

internal sealed class PineconeMetricConverter : JsonConverter<PineconeMetric>
{
    public override PineconeMetric Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        var value = reader.ValueSpan;

        // Bitwise to lowercase
        return (value[0] | 32) switch
        {
            (byte)'c' when "cosine"u8.SequenceEqual(value) => PineconeMetric.Cosine,
            (byte)'d' when "dotproduct"u8.SequenceEqual(value) => PineconeMetric.DotProduct,
            (byte)'e' when "euclidean"u8.SequenceEqual(value) => PineconeMetric.Euclidean,
            _ => throw new SerializationException("Unknown enum value")
        };
    }

    public override void Write(
        Utf8JsonWriter writer,
        PineconeMetric value,
        JsonSerializerOptions options)
    {
        writer.WriteStringValue(value switch
        {
            PineconeMetric.Cosine => "cosine"u8,
            PineconeMetric.DotProduct => "dotproduct"u8,
            PineconeMetric.Euclidean => "euclidean"u8,
            _ => throw new SerializationException("Unknown enum value")
        });
    }
}