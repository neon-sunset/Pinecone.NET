using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Pinecone;

internal static class Extensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static ValueTask CheckStatusCode(
        this HttpResponseMessage response, CancellationToken ct, [CallerMemberName] string requestName = "")
    {
#if NETSTANDARD2_0
        return response.IsSuccessStatusCode ? default : ThrowOnFailedResponse(response, requestName, ct);
#else
        return response.IsSuccessStatusCode ? ValueTask.CompletedTask : ThrowOnFailedResponse(response, requestName, ct);
#endif

        [DoesNotReturn, StackTraceHidden]
        static async ValueTask ThrowOnFailedResponse(
            HttpResponseMessage response, string requestName, CancellationToken ct)
        {

            throw new HttpRequestException($"{requestName} request has failed. " +
#if NETSTANDARD2_0
                $"Code: {response.StatusCode}. Message: {await response.Content.ReadAsStringAsync().ConfigureAwait(false)}");
#else
                $"Code: {response.StatusCode}. Message: {await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false)}");
#endif
        }
    }
}
