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
        => JsonSerializer.Deserialize<Dictionary<string, string>>(
            File.ReadAllText(PathHelper.GetSecretsPathFromSecretsId(
                typeof(UserSecretsExtensions).Assembly.GetCustomAttribute<UserSecretsIdAttribute>()!
                    .UserSecretsId)))!.ContainsKey(PineconeApiKeyUserSecretEntry);
}