using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Grpc.Core;
using Microsoft.Extensions.Http.Logging;
using Microsoft.Extensions.Logging;

namespace Pinecone;

static class Extensions
{
    internal static void AddPineconeHeaders(this HttpClient http, string apiKey)
    {
        var headers = http.DefaultRequestHeaders;

        if (!headers.Contains(Constants.RestApiKey))
            headers.Add(Constants.RestApiKey, apiKey);
        if (!headers.Contains("X-Pinecone-Api-Version"))
            headers.Add("X-Pinecone-Api-Version", Constants.ApiVersion);
        if (!headers.Contains("User-Agent"))
            headers.TryAddWithoutValidation("User-Agent", Constants.UserAgent);
    }

    internal static Metadata WithPineconeProps(this Metadata metadata, string apiKey)
    {
        metadata.Add(Constants.GrpcApiKey, apiKey);
        metadata.Add("X-Pinecone-Api-Version", Constants.ApiVersion);
        metadata.Add("User-Agent", Constants.UserAgent);

        return metadata;
    }

    internal static HttpMessageHandler CreateLoggingHandler(this ILoggerFactory factory)
    {
        return new LoggingHttpMessageHandler(
            factory.CreateLogger<HttpClient>(),
            Constants.RedactApiKeyOptions)
        { InnerHandler = new HttpClientHandler() };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static ValueTask CheckStatusCode(this HttpResponseMessage response, CancellationToken ct, [CallerMemberName] string requestName = "")
    {
#if NETSTANDARD2_0
        return response.IsSuccessStatusCode ? default : ThrowOnFailedResponse(response, requestName, ct);
#else
        return response.IsSuccessStatusCode ? ValueTask.CompletedTask : ThrowOnFailedResponse(response, requestName, ct);
#endif

        [DoesNotReturn, StackTraceHidden]
        static async ValueTask ThrowOnFailedResponse(HttpResponseMessage response, string requestName, CancellationToken ct)
        {
            var message = $"{requestName} request has failed. " +
#if NETSTANDARD2_0
                $"Code: {response.StatusCode}. Message: {await response.Content.ReadAsStringAsync().ConfigureAwait(false)}";
#else
                $"Code: {response.StatusCode}. Message: {await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false)}";
#endif
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
