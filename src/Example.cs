using Grpc.Net.Client;

namespace Pinecone
{
    internal class Example
    {
        public void Test()
        {
            var channel = GrpcChannel.ForAddress("127.0.0.1");
            var client = new VectorService.VectorServiceClient(channel);
        }
    }
}
