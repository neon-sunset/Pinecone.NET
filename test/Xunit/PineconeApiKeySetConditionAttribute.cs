namespace PineconeTests.Xunit;

public sealed class PineconeApiKeySetConditionAttribute : Attribute, ITestCondition
{
    public ValueTask<bool> IsMetAsync()
    {
        var isMet = UserSecretsExtensions.ContainsPineconeApiKey();

        return ValueTask.FromResult(isMet);
    }

    public string SkipReason
        => $"Pinecone API key was not specified in user secrets. Use the following command to set it: dotnet user-secrets set \"{UserSecretsExtensions.PineconeApiKeyUserSecretEntry}\" \"[your Pinecone API key]\"";
}