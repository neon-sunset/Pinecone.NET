using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Pinecone.Grpc;
using Pinecone.Rest;

namespace Pinecone.Playground;

[MemoryDiagnoser]
[SimpleJob(iterationCount: 15)]
[SimpleJob(RuntimeMoniker.NativeAot80, iterationCount: 15)]
public class Benchmark
{
    private static readonly PineconeClient Client = new(Environment.GetEnvironmentVariable("PINECONE_KEY")!, "us-east4-gcp");
    private static readonly Index<GrpcTransport> Grpc = Client.GetIndex("playground").Result;
    private static readonly Index<RestTransport> Rest = Client.GetIndex<RestTransport>("playground").Result;

    [Benchmark]
    public async Task GrpcStats()
    {
        await Grpc.DescribeStats();
    }

    [Benchmark]
    public async Task RestStats()
    {
        await Rest.DescribeStats();
    }

    [Benchmark]
    public async Task GrpcQuery()
    {
        await Grpc.Query("helloworld", 10, includeMetadata: true);
    }

    [Benchmark]
    public async Task RestQuery()
    {
        await Rest.Query("helloworld", 10, includeMetadata: true);
    }
}
