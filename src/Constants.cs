using Microsoft.Extensions.Http;

namespace Pinecone;

internal static class Constants
{
    public const string RestApiKey = "Api-Key";
    public const string GrpcApiKey = "api-key";

    public static readonly string Version =
        typeof(Constants).Assembly.GetName().Version?.ToString(3) ?? "0.0.0";

    public static readonly HttpClientFactoryOptions RedactApiKeyOptions = new()
    {
        ShouldRedactHeaderValue = h => h.Equals(RestApiKey, StringComparison.OrdinalIgnoreCase)
    };
}
