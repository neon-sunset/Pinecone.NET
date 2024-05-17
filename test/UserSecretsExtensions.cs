using System.Reflection;
using System.Text.Json;
using Microsoft.Extensions.Configuration.UserSecrets;

namespace PineconeTests;

public static class UserSecretsExtensions
{
    public const string PineconeApiKeyUserSecretEntry = "PineconeApiKey";

    public static string ReadPineconeApiKey()
        => JsonSerializer.Deserialize<Dictionary<string, string>>(
            File.ReadAllText(PathHelper.GetSecretsPathFromSecretsId(
                typeof(UserSecretsExtensions).Assembly.GetCustomAttribute<UserSecretsIdAttribute>()!
                    .UserSecretsId)))![PineconeApiKeyUserSecretEntry];

    public static bool ContainsPineconeApiKey()
    {
        var userSecretsIdAttribute = typeof(UserSecretsExtensions).Assembly.GetCustomAttribute<UserSecretsIdAttribute>();
        if (userSecretsIdAttribute == null)
        {
            return false;
        }

        return JsonSerializer.Deserialize<Dictionary<string, string>>(
            File.ReadAllText(PathHelper.GetSecretsPathFromSecretsId(
                userSecretsIdAttribute.UserSecretsId)))!.ContainsKey(PineconeApiKeyUserSecretEntry);
    }
}