using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace Pinecone;

internal static class ThrowHelpers
{
    [DoesNotReturn, StackTraceHidden]
    internal static T JsonException<T>(string? message = null) =>
        throw new JsonException(message ?? $"Failed to deserialize {typeof(T)}");
}
