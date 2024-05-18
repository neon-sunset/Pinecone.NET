using Xunit;
using Xunit.Sdk;

namespace PineconeTests.Xunit;

[AttributeUsage(AttributeTargets.Method)]
[XunitTestCaseDiscoverer("PineconeTests.Xunit.PineconeTheoryDiscoverer", "PineconeTests")]
public sealed class PineconeTheoryAttribute : TheoryAttribute;