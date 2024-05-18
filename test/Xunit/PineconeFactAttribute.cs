using Xunit;
using Xunit.Sdk;

namespace PineconeTests.Xunit;

[AttributeUsage(AttributeTargets.Method)]
[XunitTestCaseDiscoverer("PineconeTests.Xunit.PineconeFactDiscoverer", "PineconeTests")]
public sealed class PineconeFactAttribute : FactAttribute;