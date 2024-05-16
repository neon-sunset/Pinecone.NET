using Pinecone;
using PineconeTests.Xunit;
using Xunit;

namespace PineconeTests;

[Collection("PineconeTests")]
[PineconeApiKeySetCondition]
public class PodBasedDataTests(PodBasedDataTests.PodBasedDataTestFixture fixture) : DataTestBase<PodBasedDataTests.PodBasedDataTestFixture>(fixture)
{
    public class PodBasedDataTestFixture : DataTestFixtureBase
    {
        protected override string IndexName => "pod-data-tests";

        protected override async Task CreateIndexAndWait()
        {
            var attemptCount = 0;
            await Pinecone.CreatePodBasedIndex(IndexName, dimension: 8, metric: Metric.Cosine, environment: "gcp-starter", podType: "starter", pods: 1);

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
