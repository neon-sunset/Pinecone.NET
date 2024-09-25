using Microsoft.Extensions.Http;

namespace Pinecone;

static class Constants
{
    public const string RestApiKey = "Api-Key";
    public const string GrpcApiKey = "api-key";

    public const string ApiVersion = "2024-07";

    public static readonly string UserAgent =
        $"lang=C#; Pinecone.NET/{typeof(Constants).Assembly.GetName().Version?.ToString(3) ?? "0.0.0"}";

    public static readonly HttpClientFactoryOptions RedactApiKeyOptions = new()
    {
        ShouldRedactHeaderValue = h => h.Equals(RestApiKey, StringComparison.OrdinalIgnoreCase)
    };
}
