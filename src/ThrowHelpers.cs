using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace Pinecone;

static class ThrowHelpers
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void CheckGreaterThan(
        int value,
        int threshold,
        [CallerArgumentExpression(nameof(value))] string name = "")
    {
        if (value <= threshold) Throw(name, threshold);

        [DoesNotReturn, StackTraceHidden]
        static void Throw(string name, int threshold) =>
            throw new ArgumentOutOfRangeException(name, $"Value must be greater than {threshold}");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void CheckNull(
        [NotNull] object? value,
        [CallerArgumentExpression(nameof(value))] string name = "")
    {
        if (value is null) Throw(name);

        [DoesNotReturn, StackTraceHidden]
        static void Throw(string name) =>
            throw new ArgumentNullException(name);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void CheckNullOrWhiteSpace(
        [NotNull] string? value,
        [CallerArgumentExpression(nameof(value))] string name = "")
    {
#if NETSTANDARD2_0
        // .IsNullOrWhiteSpace does not have nullability annotation on NS2.0
        if (value is null || string.IsNullOrWhiteSpace(value)) Throw(name);
#else
        if (string.IsNullOrWhiteSpace(value)) Throw(name);
#endif

        [DoesNotReturn, StackTraceHidden]
        static void Throw(string name) =>
            throw new ArgumentException("Value cannot be null or whitespace", name);
    }

    [DoesNotReturn, StackTraceHidden]
    internal static void ArgumentException(string message) =>
        throw new ArgumentException(message);

    [DoesNotReturn, StackTraceHidden]
    internal static T ArgumentException<T>(string message) =>
        throw new ArgumentException(message);

    [DoesNotReturn, StackTraceHidden]
    internal static void FormatException(string message) =>
        throw new FormatException(message);

    [DoesNotReturn, StackTraceHidden]
    internal static T FormatException<T>(string message) =>
        throw new FormatException(message);

    [DoesNotReturn, StackTraceHidden]
    internal static T JsonException<T>(string? message = null) =>
        throw new JsonException(message ?? $"Failed to deserialize {typeof(T)}");
}
