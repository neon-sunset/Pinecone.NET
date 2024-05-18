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
        return response.IsSuccessStatusCode ? ValueTask.CompletedTask : ThrowOnFailedResponse(response, requestName, ct);

        [DoesNotReturn, StackTraceHidden]
        static async ValueTask ThrowOnFailedResponse(
            HttpResponseMessage response, string requestName, CancellationToken ct)
        {
            throw new HttpRequestException($"{requestName} request has failed. " +
                $"Code: {response.StatusCode}. Message: {await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false)}");
        }
    }
}
