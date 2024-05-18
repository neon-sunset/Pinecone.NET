namespace PineconeTests.Xunit;

public interface ITestCondition
{
    ValueTask<bool> IsMetAsync();

    string SkipReason { get; }
}