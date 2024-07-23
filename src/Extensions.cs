using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Pinecone;

internal static class Extensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static ValueTask CheckStatusCode(
        this HttpResponseMessage response, ILogger logger, string operationName, CancellationToken ct, [CallerMemberName] string requestName = "")
    {
#if NETSTANDARD2_0
        return response.IsSuccessStatusCode ? default : ThrowOnFailedResponse(response, requestName, logger, operationName, ct);
#else
        return response.IsSuccessStatusCode ? ValueTask.CompletedTask : ThrowOnFailedResponse(response, requestName, logger, operationName, ct);
#endif

        [DoesNotReturn, StackTraceHidden]
        static async ValueTask ThrowOnFailedResponse(
            HttpResponseMessage response, string requestName, ILogger logger, string operationName, CancellationToken ct)
        {
            var message = $"{requestName} request has failed. " +
#if NETSTANDARD2_0
                $"Code: {response.StatusCode}. Message: {await response.Content.ReadAsStringAsync().ConfigureAwait(false)}";
#else
                $"Code: {response.StatusCode}. Message: {await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false)}";
#endif
            logger.OperationFailed(operationName, message);

            throw new HttpRequestException(message);
        }
    }

#if NETSTANDARD2_0
    internal static void Deconstruct<T1, T2>(this KeyValuePair<T1, T2> tuple, out T1 key, out T2 value)
    {
        key = tuple.Key;
        value = tuple.Value;
    }
#endif
}