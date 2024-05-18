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
                    .UserSecretsId)))![PineconeApiKeyUserSecretEntry].Trim();

    public static bool ContainsPineconeApiKey()
    {
        var userSecretsIdAttribute = typeof(UserSecretsExtensions).Assembly.GetCustomAttribute<UserSecretsIdAttribute>();
        if (userSecretsIdAttribute == null)
        {
            return false;
        }

        var path = PathHelper.GetSecretsPathFromSecretsId(userSecretsIdAttribute.UserSecretsId);
        if (!File.Exists(path))
        {
            return false;
        }

        return JsonSerializer.Deserialize<Dictionary<string, string>>(
            File.ReadAllText(path))!.ContainsKey(PineconeApiKeyUserSecretEntry);
    }
}