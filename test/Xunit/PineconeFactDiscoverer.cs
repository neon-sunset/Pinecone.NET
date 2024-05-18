using Xunit.Abstractions;
using Xunit.Sdk;

namespace PineconeTests.Xunit;

public class PineconeFactDiscoverer(IMessageSink messageSink) : FactDiscoverer(messageSink)
{
    protected override IXunitTestCase CreateTestCase(
        ITestFrameworkDiscoveryOptions discoveryOptions,
        ITestMethod testMethod,
        IAttributeInfo factAttribute)
        => new PineconeFactTestCase(
            DiagnosticMessageSink,
            discoveryOptions.MethodDisplayOrDefault(),
            discoveryOptions.MethodDisplayOptionsOrDefault(),
            testMethod);
}