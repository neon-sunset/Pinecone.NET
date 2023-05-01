using System.Text.Json;
using System.Text.Json.Serialization;
using CommunityToolkit.Diagnostics;

namespace Pinecone.Transport.Rest;

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
            (byte)'c' when value.SequenceEqual("cosine"u8) => PineconeMetric.Cosine,
            (byte)'d' when value.SequenceEqual("dotproduct"u8) => PineconeMetric.DotProduct,
            (byte)'e' when value.SequenceEqual("euclidean"u8) => PineconeMetric.Euclidean,
            _ => ThrowHelper.ThrowArgumentOutOfRangeException<PineconeMetric>("Unknown enum value")
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
            _ => ThrowHelper.ThrowArgumentOutOfRangeException<byte[]>("Unknown enum value")
        });
    }
}

internal sealed class PineconeIndexStateConverter : JsonConverter<PineconeIndexState>
{
    public override PineconeIndexState Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
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

    public override void Write(
        Utf8JsonWriter writer,
        PineconeIndexState value,
        JsonSerializerOptions options)
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