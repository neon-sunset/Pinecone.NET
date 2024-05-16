using Pinecone;
using PineconeTests.Xunit;
using Xunit;

namespace PineconeTests;

[Collection("PineconeTests")]
[PineconeApiKeySetCondition]
public class ServerlessDataTests(ServerlessDataTests.ServerlessDataTestFixture fixture) : DataTestBase<ServerlessDataTests.ServerlessDataTestFixture>(fixture)
{
    public class ServerlessDataTestFixture : DataTestFixtureBase
    {
        protected override string IndexName => "serverless-data-tests";

        protected override async Task CreateIndexAndWait()
        {
            var attemptCount = 0;
            await Pinecone.CreateServerlessIndex(IndexName, dimension: 8, metric: Metric.Cosine, cloud: "aws", region: "us-east-1");

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
