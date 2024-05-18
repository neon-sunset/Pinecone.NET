using PineconeTests.Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

public sealed class PineconeTheoryTestCase : XunitTheoryTestCase
{
    [Obsolete("Called by the de-serializer; should only be called by deriving classes for de-serialization purposes")]
    public PineconeTheoryTestCase()
    {
    }

    public PineconeTheoryTestCase(
        IMessageSink diagnosticMessageSink,
        TestMethodDisplay defaultMethodDisplay,
        TestMethodDisplayOptions defaultMethodDisplayOptions,
        ITestMethod testMethod)
        : base(diagnosticMessageSink, defaultMethodDisplay, defaultMethodDisplayOptions, testMethod)
    {
    }

    public override async Task<RunSummary> RunAsync(
        IMessageSink diagnosticMessageSink,
        IMessageBus messageBus,
        object[] constructorArguments,
        ExceptionAggregator aggregator,
        CancellationTokenSource ctSource)
        => await XunitTestCaseExtensions.TrySkipAsync(this, messageBus)
            ? new RunSummary { Total = 1, Skipped = 1 }
            : await base.RunAsync(
                diagnosticMessageSink,
                messageBus,
                constructorArguments,
                aggregator,
                ctSource);
}