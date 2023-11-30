using System.Buffers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using CommunityToolkit.Diagnostics;

namespace Pinecone.Rest;

// Co-implemented with Bing Chat :D
public class IndexNamespaceArrayConverter : JsonConverter<IndexNamespace[]>
{
    public override IndexNamespace[] Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            ThrowHelper.ThrowFormatException("Expected object element");
        }

        // TODO: Remove intermediate allocation
        var buffer = new List<IndexNamespace>();

        while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
        {
            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                ThrowHelper.ThrowFormatException("Expected property name");
            }

            ReadOnlySpan<byte> nameSpan;
            if (!reader.HasValueSequence)
            {
                nameSpan = reader.ValueSpan;
            }
            else
            {
                var nameBuf = new byte[reader.ValueSequence.Length];
                reader.ValueSequence.CopyTo(nameBuf);
                nameSpan = nameBuf;
            }

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

        return [..buffer];
    }

    public override void Write(Utf8JsonWriter writer, IndexNamespace[] value, JsonSerializerOptions options)
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
            case IEnumerable<MetadataValue> e: WriteEnumerable(writer, e); break;
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

    private static void WriteEnumerable(Utf8JsonWriter writer, IEnumerable<MetadataValue> enumerable)
    {
        writer.WriteStartArray();
        foreach (var value in enumerable)
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
