using Xunit.Abstractions;
using Xunit.Sdk;

namespace PineconeTests.Xunit;

public sealed class PineconeFactTestCase : XunitTestCase
{
    [Obsolete("Called by the de-serializer; should only be called by deriving classes for de-serialization purposes")]
    public PineconeFactTestCase()
    {
    }

    public PineconeFactTestCase(
        IMessageSink diagnosticMessageSink,
        TestMethodDisplay defaultMethodDisplay,
        TestMethodDisplayOptions defaultMethodDisplayOptions,
        ITestMethod testMethod,
        object[]? testMethodArguments = null)
        : base(diagnosticMessageSink, defaultMethodDisplay, defaultMethodDisplayOptions, testMethod, testMethodArguments)
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