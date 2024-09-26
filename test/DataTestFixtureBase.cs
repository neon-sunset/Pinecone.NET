using Pinecone;
using Xunit;

namespace PineconeTests;
public abstract class DataTestFixtureBase<T> : IAsyncLifetime
    where T : ITransport<T>
{
    public const int MaxAttemptCount = 100;
    public const int DelayInterval = 300;
    public abstract string IndexName { get; }

    public PineconeClient Pinecone { get; private set; } = null!;

    public virtual Index<T> Index { get; set; } = null!;

    public virtual async Task InitializeAsync()
    {
        Pinecone = new PineconeClient(UserSecretsExtensions.ReadPineconeApiKey());

        await ClearIndexesAsync();
        await CreateIndexAndWait();
        await AddSampleDataAsync();
    }

    protected abstract Task CreateIndexAndWait();

    public async Task DisposeAsync()
    {
        if (Pinecone is not null)
        {
            await ClearIndexesAsync();
            Pinecone.Dispose();
        }
    }

    private async Task AddSampleDataAsync()
    {
        var basicVectors = Enumerable.Range(1, 5).Select(i => new Vector
        {
            Id = "basic-vector-" + i,
            Values = new[] { i * 0.5f, i * 1.0f, i * 1.5f, i * 2.0f, i * 2.5f, i * 3.0f, i * 3.5f, i * 4.0f },
        }).ToList();

        await InsertAndWait(basicVectors);

        var customNamespaceVectors = Enumerable.Range(1, 3).Select(i => new Vector
        {
            Id = "custom-namespace-vector-" + i,
            Values = new[] { i * 1.1f, i * 2.2f, i * 3.3f, i * 4.4f, i * 5.5f, i * 6.6f, i * 7.7f, i * 8.8f },
        }).ToList();

        await InsertAndWait(customNamespaceVectors, "namespace1");

        var metadata1 = new MetadataMap
        {
            ["type"] = "number set",
            ["subtype"] = "primes",
            ["rank"] = 3,
            ["overhyped"] = false,
            ["list"] = new string[] { "2", "1" },
        };

        var metadata2 = new MetadataMap
        {
            ["type"] = "number set",
            ["subtype"] = "fibo",
            ["list"] = new string[] { "0", "1" },
        };

        var metadata3 = new MetadataMap
        {
            ["type"] = "number set",
            ["subtype"] = "lucas",
            ["rank"] = 12,
            ["overhyped"] = false,
            ["list"] = new string[] { "two", "one" },
        };

        var metadataVectors = new Vector[]
        {
            new() { Id = "metadata-vector-1", Values = new[] { 2f, 3, 5, 7, 11, 13, 17, 19 }, Metadata = metadata1 },
            new() { Id = "metadata-vector-2", Values = new[] { 0f, 1, 1, 2, 3, 5, 8, 13 }, Metadata = metadata2 },
            new() { Id = "metadata-vector-3", Values = new[] { 2f, 1, 3, 4, 7, 11, 18, 29 }, Metadata = metadata3 },
        };

        await InsertAndWait(metadataVectors);

        var sparseVectors = new Vector[]
        {
            new() { Id = "sparse-1", Values = new[] { 5f, 10, 15, 20, 25, 30, 35, 40 }, SparseValues = new() { Indices = new[] { 1u, 4u }, Values = new[] { 0.2f, 0.5f } } },
            new() { Id = "sparse-2", Values = new[] { 15f, 110, 115, 120, 125, 130, 135, 140 }, SparseValues = new() { Indices = new[] { 2u, 3u }, Values = new[] { 0.5f, 0.8f } } },
        };

        await InsertAndWait(sparseVectors);
    }

    public virtual async Task<uint> InsertAndWait(IEnumerable<Vector> vectors, string? indexNamespace = null)
    {
        // NOTE: this only works when inserting *new* vectors, if the vector already exisits the new vector count won't match
        // and we will have false-negative "failure" to insert
        var stats = await Index.DescribeStats();
        var vectorCountBefore = stats.TotalVectorCount;
        var attemptCount = 0;
        var result = await Index.Upsert(vectors, indexNamespace);

        do
        {
            await Task.Delay(DelayInterval);
            attemptCount++;
            stats = await Index.DescribeStats();
        } while (stats.TotalVectorCount < vectorCountBefore + vectors.Count() && attemptCount <= MaxAttemptCount);

        if (stats.TotalVectorCount < vectorCountBefore + vectors.Count())
        {
            throw new InvalidOperationException("'Upsert' operation didn't complete in time. Vectors count: " + vectors.Count());
        }

        return result;
    }

    public async Task DeleteAndWait(IEnumerable<string> ids, string? indexNamespace = null)
    {
        indexNamespace ??= "";

        var stats = await Index.DescribeStats();
        var vectorCountBefore = stats.Namespaces.Single(x => x.Name == indexNamespace).VectorCount;

        var attemptCount = 0;
        await Index.Delete(ids, indexNamespace);
        long vectorCount;
        do
        {
            await Task.Delay(DelayInterval);
            attemptCount++;
            stats = await Index.DescribeStats();
            // When using REST transport, it appears that the namespace is no longer returned in the stats
            // if the last vector(s) are deleted from it. Here, we handle that by treating it as zero.
            vectorCount = stats.Namespaces
                .SingleOrDefault(
                    x => x.Name == indexNamespace,
                    new() { Name = indexNamespace, VectorCount = 0 })
                .VectorCount;
        } while (vectorCount > vectorCountBefore - ids.Count() && attemptCount <= MaxAttemptCount);

        if (vectorCount > vectorCountBefore - ids.Count())
        {
            throw new InvalidOperationException("'Delete' operation didn't complete in time.");
        }
    }

    private async Task ClearIndexesAsync()
    {
        var indexes = await Pinecone.ListIndexes();
        var deletions = indexes.Select(x => DeleteExistingIndexAndWaitAsync(x.Name));

        await Task.WhenAll(deletions);
    }

    private async Task DeleteExistingIndexAndWaitAsync(string indexName)
    {
        var exists = true;
        try
        {
            var attemptCount = 0;
            await Pinecone.DeleteIndex(indexName);

            do
            {
                await Task.Delay(DelayInterval);
                var indexes = (await Pinecone.ListIndexes()).Select(x => x.Name).ToArray();
                if (indexes.Length == 0 || !indexes.Contains(indexName))
                {
                    exists = false;
                }
            } while (exists && attemptCount <= MaxAttemptCount);
        }
        // TODO: This is a questionable workaround but does the job for now
        catch (HttpRequestException ex) when (ex.Message.Contains("NOT_FOUND"))
        {
            // index was already deleted
            exists = false;
        }

        if (exists)
        {
            throw new InvalidOperationException("'Delete index' operation didn't complete in time. Index name: " + indexName);
        }
    }
}
