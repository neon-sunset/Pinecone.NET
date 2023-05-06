using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using CommunityToolkit.Diagnostics;

namespace Pinecone.Rest;

internal sealed class PineconeMetricConverter : JsonConverter<PineconeMetric>
{
    public override PineconeMetric Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.ValueSpan;

        // Bitwise to lowercase
        return (value[0] | 32) switch
        {
            (byte)'c' when value.SequenceEqual("cosine"u8) => PineconeMetric.Cosine,
            (byte)'d' when value.SequenceEqual("dotproduct"u8) => PineconeMetric.DotProduct,
            (byte)'e' when value.SequenceEqual("euclidean"u8) => PineconeMetric.Euclidean,
            _ => ThrowHelper.ThrowArgumentOutOfRangeException<PineconeMetric>("Unknown enum value")
        };
    }

    public override void Write(Utf8JsonWriter writer, PineconeMetric value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value switch
        {
            PineconeMetric.Cosine => "cosine"u8,
            PineconeMetric.DotProduct => "dotproduct"u8,
            PineconeMetric.Euclidean => "euclidean"u8,
            _ => ThrowHelper.ThrowArgumentOutOfRangeException<byte[]>("Unknown enum value")
        });
    }
}

internal sealed class PineconeIndexStateConverter : JsonConverter<PineconeIndexState>
{
    public override PineconeIndexState Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.ValueSpan;
        return value switch
        {
            [(byte)'I', ..] when value.SequenceEqual("Initializing"u8) => PineconeIndexState.Initializing,
            [(byte)'S', ..] when value.SequenceEqual("ScalingUp"u8) => PineconeIndexState.ScalingUp,
            [(byte)'S', ..] when value.SequenceEqual("ScalingDown"u8) => PineconeIndexState.ScalingDown,
            [(byte)'T', ..] when value.SequenceEqual("Terminating"u8) => PineconeIndexState.Terminating,
            [(byte)'R', ..] when value.SequenceEqual("Ready"u8) => PineconeIndexState.Ready,
            _ => ThrowHelper.ThrowArgumentOutOfRangeException<PineconeIndexState>("Unknown enum value")
        };
    }

    public override void Write(Utf8JsonWriter writer, PineconeIndexState value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value switch
        {
            PineconeIndexState.Initializing => "Initializing"u8,
            PineconeIndexState.ScalingUp => "ScalingUp"u8,
            PineconeIndexState.ScalingDown => "ScalingDown"u8,
            PineconeIndexState.Terminating => "Terminating"u8,
            PineconeIndexState.Ready => "Ready"u8,
            _ => ThrowHelper.ThrowArgumentOutOfRangeException<byte[]>("Unknown enum value")
        });
    }
}

// Co-implemented by Bing Chat :D
public class PineconeIndexNamespaceArrayConverter : JsonConverter<PineconeIndexNamespace[]>
{
    public override PineconeIndexNamespace[] Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            ThrowHelper.ThrowFormatException("Expected object element");
        }

        // TODO: Remove intermediate allocation
        var buffer = new List<PineconeIndexNamespace>();

        while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
        {
            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                ThrowHelper.ThrowFormatException("Expected property name");
            }

            var nameSpan = reader.ValueSpan;

            reader.Read();
            if (reader.TokenType != JsonTokenType.StartObject)
            {
                ThrowHelper.ThrowFormatException("Expected object element");
            }

            while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
            {
                if (reader.TokenType != JsonTokenType.PropertyName || !reader.ValueTextEquals("vectorCount"u8))
                {
                    ThrowHelper.ThrowFormatException("Expected 'vectorCount' property");
                }

                reader.Read();
                if (reader.TokenType != JsonTokenType.Number)
                {
                    ThrowHelper.ThrowFormatException("Expected number value");
                }

                buffer.Add(new() { Name = Encoding.UTF8.GetString(nameSpan), VectorCount = reader.GetUInt32() });
            }
        }

        return buffer.ToArray();
    }

    public override void Write(Utf8JsonWriter writer, PineconeIndexNamespace[] value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        foreach (var indexNamespace in value)
        {
            writer.WritePropertyName(indexNamespace.Name);
            writer.WriteStartObject();
            writer.WriteNumber("vectorCount"u8, indexNamespace.VectorCount);
            writer.WriteEndObject();
        }

        writer.WriteEndObject();
    }
}

public class MetadataValueConverter : JsonConverter<MetadataValue>
{
    public override MetadataValue Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        ReadValue(ref reader);

    public override void Write(Utf8JsonWriter writer, MetadataValue value, JsonSerializerOptions options) =>
        WriteValue(writer, value);

    private static MetadataValue ReadValue(ref Utf8JsonReader reader)
    {
        return reader.TokenType switch
        {
            JsonTokenType.Null => default,
            JsonTokenType.True => true,
            JsonTokenType.False => false,
            JsonTokenType.String => reader.GetString(),
            JsonTokenType.Number => reader.GetDouble(),
            JsonTokenType.StartArray => ReadArray(ref reader),
            JsonTokenType.StartObject => ReadMap(ref reader),
            _ => ThrowHelper.ThrowFormatException<MetadataValue>($"Unexpected token type {reader.TokenType}")
        };
    }

    private static MetadataValue[] ReadArray(ref Utf8JsonReader reader)
    {
        return JsonSerializer.Deserialize(ref reader, SerializerContext.Default.MetadataValueArray)
            ?? ThrowHelpers.JsonException<MetadataValue[]>();
    }

    private static MetadataMap ReadMap(ref Utf8JsonReader reader)
    {
        return JsonSerializer.Deserialize(ref reader, SerializerContext.Default.MetadataMap)
            ?? ThrowHelpers.JsonException<MetadataMap>();
    }

    private static void WriteValue(Utf8JsonWriter writer, MetadataValue value)
    {
        switch (value.Inner)
        {
            case null: writer.WriteNullValue(); break;
            case bool b: writer.WriteBooleanValue(b); break;
            case string s: writer.WriteStringValue(s); break;
            case double d: writer.WriteNumberValue(d); break;
            case MetadataValue[] a: WriteArray(writer, a); break;
            case MetadataMap m: WriteMap(writer, m); break;
            default:
                ThrowHelper.ThrowArgumentOutOfRangeException(
                    nameof(value.Inner), $"Unknown MetadataValue of type {value.Inner.GetType()}");
                break;
        }
    }

    private static void WriteArray(Utf8JsonWriter writer, MetadataValue[] array)
    {
        writer.WriteStartArray();
        foreach (var value in array)
        {
            WriteValue(writer, value);
        }
        writer.WriteEndArray();
    }

    private static void WriteMap(Utf8JsonWriter writer, MetadataMap map)
    {
        writer.WriteStartObject();
        foreach (var (key, value) in map)
        {
            writer.WritePropertyName(key);
            WriteValue(writer, value);
        }
        writer.WriteEndObject();
    }
}
