using Pinecone;
using Pinecone.Grpc;
using Xunit;

namespace PineconeTests;

[Collection("PineconeTests")]
public class IndexTests
{
    private const int MaxAttemptCount = 300;
    private const int DelayInterval = 100;

    [Fact]
    public async Task Legacy_index_sandbox()
    {
        var indexName = "legacy-pod-based-index";

        var pinecone = new PineconeClient(UserSecrets.Read("PineconeApiKey"));

        // check all existing indexes
        var podIndexes = (await pinecone.ListIndexes()).Where(x => x.Spec.Pod is not null).Select(x => x.Name).ToList();
        foreach (var podIndex in podIndexes)
        {
            // delete the previous pod-based index (only one is allowed on free plan)
            await pinecone.DeleteIndex(indexName);
        }

        var attemptCount = 0;
        // wait until old index has been deleted
        do
        {
            await Task.Delay(DelayInterval);
            attemptCount++;
            podIndexes = (await pinecone.ListIndexes()).Where(x => x.Spec.Pod is not null).Select(x => x.Name).ToList();
        }
        while (podIndexes.Any() && attemptCount < MaxAttemptCount);

        //this will get created but initialization fails later
        await pinecone.CreatePodBasedIndex(indexName, 3, Metric.Cosine, "gcp-starter", "starter", 1);

        var listIndexes = await pinecone.ListIndexes();

        Assert.Contains(indexName, listIndexes.Select(x => x.Name));
    }

    [Theory]
    [InlineData(Metric.DotProduct)]
    [InlineData(Metric.Cosine)]
    [InlineData(Metric.Euclidean)]
    public async Task Create_and_delete_serverless_index(Metric metric)
    {
        var indexName = "serverless-index";

        var pinecone = new PineconeClient(UserSecrets.Read("PineconeApiKey"));

        // check for existing index
        var podIndexes = (await pinecone.ListIndexes()).Select(x => x.Name).ToList();
        if (podIndexes.Contains(indexName))
        {
            // delete the previous index
            await pinecone.DeleteIndex(indexName);
        }

        var attemptCount = 0;
        // wait until old index has been deleted
        do
        {
            await Task.Delay(DelayInterval);
            attemptCount++;
            podIndexes = (await pinecone.ListIndexes()).Select(x => x.Name).ToList();
        }
        while (podIndexes.Contains(indexName) && attemptCount < MaxAttemptCount);

        await pinecone.CreateServerlessIndex(indexName, 3, metric, "aws", "us-east-1");

        Index<GrpcTransport> index;
        attemptCount = 0;
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

    [Fact]
    public async Task List_collections()
    {
        var pinecone = new PineconeClient(UserSecrets.Read("PineconeApiKey"));
        var collections = await pinecone.ListCollections();
    }
}
