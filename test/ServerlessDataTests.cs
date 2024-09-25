using Pinecone;
using Pinecone.Grpc;
using Pinecone.Rest;
using PineconeTests.Xunit;
using Xunit;

namespace PineconeTests;

[Collection("PineconeTests")]
[PineconeApiKeySetCondition]
public class ServerlessRestTransportDataTests(ServerlessRestTransportDataTests.ServerlessDataTestFixture fixture)
    : DataTestBase<ServerlessRestTransportDataTests.ServerlessDataTestFixture, RestTransport>(fixture)
{
    public class ServerlessDataTestFixture : DataTestFixtureBase<RestTransport>
    {
        public override string IndexName => "serverless-rest-data-tests";

        protected override async Task CreateIndexAndWait()
        {
            var attemptCount = 0;
            await Pinecone.CreateServerlessIndex(IndexName, dimension: 8, metric: Metric.DotProduct, cloud: "aws", region: "us-east-1");

            do
            {
                await Task.Delay(DelayInterval);
                attemptCount++;
                Index = await Pinecone.GetIndex(IndexName);
            } while (!Index.Status.IsReady && attemptCount <= MaxAttemptCount);

            if (!Index.Status.IsReady)
            {
                throw new InvalidOperationException("'Create index' operation didn't complete in time. Index name: " + IndexName);
            }
        }
    }
}

[Collection("PineconeTests")]
[PineconeApiKeySetCondition]
public class ServerlessGrpcTransportDataTests(ServerlessGrpcTransportDataTests.ServerlessDataTestFixture fixture)
    : DataTestBase<ServerlessGrpcTransportDataTests.ServerlessDataTestFixture, GrpcTransport>(fixture)
{
    public class ServerlessDataTestFixture : DataTestFixtureBase<GrpcTransport>
    {
        public override string IndexName => "serverless-grpc-data-tests";

        protected override async Task CreateIndexAndWait()
        {
            var attemptCount = 0;
            await Pinecone.CreateServerlessIndex(IndexName, dimension: 8, metric: Metric.DotProduct, cloud: "aws", region: "us-east-1");

            do
            {
                await Task.Delay(DelayInterval);
                attemptCount++;
                Index = await Pinecone.GetIndex<GrpcTransport>(IndexName);
            } while (!Index.Status.IsReady && attemptCount <= MaxAttemptCount);

            if (!Index.Status.IsReady)
            {
                throw new InvalidOperationException("'Create index' operation didn't complete in time. Index name: " + IndexName);
            }
        }
    }
}
