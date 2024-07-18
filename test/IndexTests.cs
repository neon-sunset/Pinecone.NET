using Pinecone;
using Pinecone.Grpc;
using PineconeTests.Xunit;
using Xunit;

namespace PineconeTests;

[Collection("PineconeTests")]
[PineconeApiKeySetCondition]
public class IndexTests
{
    private const int MaxAttemptCount = 300;
    private const int DelayInterval = 100;

    [PineconeTheory]
    [InlineData(Metric.DotProduct, true)]
    [InlineData(Metric.Cosine, true)]
    [InlineData(Metric.Euclidean, true)]
    [InlineData(Metric.DotProduct, false, Skip = "Test environment uses free tier which does not support pod-based indexes.")]
    [InlineData(Metric.Cosine, false, Skip = "Test environment uses free tier which does not support pod-based indexes.")]
    [InlineData(Metric.Euclidean, false, Skip = "Test environment uses free tier which does not support pod-based indexes.")]
    public async Task Create_and_delete_index(Metric metric, bool serverless)
    {
        var indexName = serverless ? "serverless-index" : "pod-based-index";

        var pinecone = new PineconeClient(UserSecretsExtensions.ReadPineconeApiKey());

        // check for existing index
        var existingIndexes = await pinecone.ListIndexes();
        if (existingIndexes.Select(x => x.Name).Contains(indexName))
        {
            // delete the previous index
            await DeleteIndexAndWait(pinecone, indexName);
        }

        // if we create pod-based index, we need to delete any previous gcp-starter indexes
        // only one pod-based index is allowed on the starter environment
        if (!serverless)
        {
            existingIndexes = await pinecone.ListIndexes();
            foreach (var existingPodBasedIndex in existingIndexes.Where(x => x.Spec.Pod?.Environment == "gcp-starter"))
            {
                await DeleteIndexAndWait(pinecone, existingPodBasedIndex.Name);
            }
        }

        if (serverless)
        {
            await pinecone.CreateServerlessIndex(indexName, 3, metric, "aws", "us-east-1");
        }
        else
        {
            await pinecone.CreatePodBasedIndex(indexName, 3, metric, "gcp-starter");
        }

        Index<GrpcTransport> index;
        var attemptCount = 0;
        do
        {
            await Task.Delay(DelayInterval);
            attemptCount++;
            index = await pinecone.GetIndex(indexName);
        }
        while (!index.Status.IsReady && attemptCount < MaxAttemptCount);

        var listIndexes = await pinecone.ListIndexes();

        // validate
        Assert.Contains(indexName, listIndexes.Select(x => x.Name));
        Assert.Equal((uint)3, index.Dimension);
        Assert.Equal(metric, index.Metric);

        // cleanup
        await pinecone.DeleteIndex(indexName);
    }

    private async Task DeleteIndexAndWait(PineconeClient pinecone, string indexName)
    {
        try
        {
            await pinecone.DeleteIndex(indexName);

            List<string> existingIndexes;
            var attemptCount = 0;
            // wait until old index has been deleted
            do
            {
                await Task.Delay(DelayInterval);
                attemptCount++;
                existingIndexes = (await pinecone.ListIndexes()).Select(x => x.Name).ToList();
            }
            while (existingIndexes.Contains(indexName) && attemptCount < MaxAttemptCount);
        }
        // TODO: This is a questionable workaround but does the job for now
        catch (HttpRequestException ex) when (ex.Message.Contains("NOT_FOUND"))
        {
            // index was already deleted
        }
    }

    [PineconeFact]
    public async Task List_collections()
    {
        var pinecone = new PineconeClient(UserSecretsExtensions.ReadPineconeApiKey());
        var collections = await pinecone.ListCollections();
    }
}
