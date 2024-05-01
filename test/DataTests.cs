using Pinecone;
using Pinecone.Grpc;
using Xunit;

namespace PineconeTests;

[Collection("PineconeTests")]
public class DataTests(DataTests.TestFixture fixture) : IClassFixture<DataTests.TestFixture>
{
    private TestFixture Fixture { get; } = fixture;

    [Fact]
    public async Task Basic_query()
    {
        var x = 0.314f;

        var results = await Fixture.Index.Query(
            [x * 0.1f, x * 0.2f, x * 0.3f, x * 0.4f, x * 0.5f, x * 0.6f, x * 0.7f, x * 0.8f],
            topK: 10);

        Assert.Equal(8, results.Length);

        results =
            await Fixture.Index.Query(
                [0.7f, 7.7f, 77.7f, 777.7f, 7777.7f, 77777.7f, 777777.7f, 7777777.7f],
                topK: 10, 
                indexNamespace: "namespace1");

        Assert.Equal(3, results.Length);
    }

    [Fact]
    public async Task Query_by_Id()
    {
        var result = await Fixture.Index.Query("basic-vector-3", topK: 2);

        // NOTE: query by id uses Approximate Nearest Neighbor, which doesn't guarantee the input vector
        // to appear in the results, so we just check the result count
        Assert.Equal(2, result.Length);
    }

    [Fact]
    public async Task Query_with_basic_metadata_filter()
    {
        var filter = new MetadataMap
        {
            ["type"] = "number set"
        };

        var result = await Fixture.Index.Query([3, 4, 5, 6, 7, 8, 9, 10], topK: 5, filter);

        Assert.Equal(3, result.Length);
        var ordered = result.OrderBy(x => x.Id).ToList();

        Assert.Equal("metadata-vector-1", ordered[0].Id);
        Assert.Equal([2, 3, 5, 7, 11, 13, 17, 19], ordered[0].Values);
        Assert.Equal("metadata-vector-2", ordered[1].Id);
        Assert.Equal([0, 1, 1, 2, 3, 5, 8, 13], ordered[1].Values);
        Assert.Equal("metadata-vector-3", ordered[2].Id);
        Assert.Equal([2, 1, 3, 4, 7, 11, 18, 29], ordered[2].Values);
    }

    [Fact]
    public async Task Query_include_metadata_in_result()
    {
        var filter = new MetadataMap
        {
            ["subtype"] = "fibo"
        };

        var result = await Fixture.Index.Query([3, 4, 5, 6, 7, 8, 9, 10], topK: 5, filter, includeMetadata: true);

        Assert.Single(result);
        Assert.Equal("metadata-vector-2", result[0].Id);
        Assert.Equal([0, 1, 1, 2, 3, 5, 8, 13], result[0].Values);
        var metadata = result[0].Metadata;
        Assert.NotNull(metadata);

        Assert.Equal("number set",metadata["type"]);
        Assert.Equal("fibo", metadata["subtype"]);

        var innerList = (MetadataValue[])metadata["list"].Inner!;
        Assert.Equal("0", innerList[0]);
        Assert.Equal("1", innerList[1]);
    }

    [Fact]
    public async Task Query_with_metadata_filter_composite()
    {
        var filter = new MetadataMap
        {
            ["type"] = "number set",
            ["overhyped"] = false
        };

        var result = await Fixture.Index.Query([3, 4, 5, 6, 7, 8, 9, 10], topK: 5, filter);

        Assert.Equal(2, result.Length);
        var ordered = result.OrderBy(x => x.Id).ToList();

        Assert.Equal("metadata-vector-1", ordered[0].Id);
        Assert.Equal([2, 3, 5, 7, 11, 13, 17, 19], ordered[0].Values);
        Assert.Equal("metadata-vector-3", ordered[1].Id);
        Assert.Equal([2, 1, 3, 4, 7, 11, 18, 29], ordered[1].Values);
    }

    [Fact]
    public async Task Query_with_metadata_list_contains()
    {
        var filter = new MetadataMap
        {
            ["rank"] = new MetadataMap() { ["$in"] = new int[] { 12, 3 } }
        };

        var result = await Fixture.Index.Query([3, 4, 5, 6, 7, 8, 9, 10], topK: 10, filter, includeMetadata: true);

        Assert.Equal(2, result.Length);
        var ordered = result.OrderBy(x => x.Id).ToList();

        Assert.Equal("metadata-vector-1", ordered[0].Id);
        Assert.Equal([2, 3, 5, 7, 11, 13, 17, 19], ordered[0].Values);
        Assert.Equal("metadata-vector-3", ordered[1].Id);
        Assert.Equal([2, 1, 3, 4, 7, 11, 18, 29], ordered[1].Values);
    }

    [Fact]
    public async Task Basic_fetch()
    {
        var results = await Fixture.Index.Fetch(["basic-vector-1", "basic-vector-3"]);
        var orderedResults = results.OrderBy(x => x.Key).ToList();
        
        Assert.Equal(2, orderedResults.Count);

        Assert.Equal("basic-vector-1", orderedResults[0].Key);
        Assert.Equal("basic-vector-1", orderedResults[0].Value.Id);
        Assert.Equal([0.5f, 1.0f, 1.5f, 2.0f, 2.5f, 3.0f, 3.5f, 4.0f], orderedResults[0].Value.Values);

        Assert.Equal("basic-vector-3", orderedResults[1].Key);
        Assert.Equal("basic-vector-3", orderedResults[1].Value.Id);
        Assert.Equal([1.5f, 3.0f, 4.5f, 6.0f, 7.5f, 9.0f, 10.5f, 12.0f], orderedResults[1].Value.Values);
    }

    [Fact]
    public async Task Basic_vector_upsert_update_delete()
    {
        var testNamespace = "upsert-update-delete-namespace";
        var newVectors = new Vector[]
            {
                new() { Id = "update-vector-id-1", Values = [1, 3, 5, 7, 9, 11, 13, 15] },
                new() { Id = "update-vector-id-2", Values = [2, 3, 5, 7, 11, 13, 17, 19] },
                new() { Id = "update-vector-id-3", Values = [2, 1, 3, 4, 7, 11, 18, 29] },
            };

        await Fixture.InsertAndWait(newVectors, testNamespace);

        var initialFetch = await Fixture.Index.Fetch(["update-vector-id-2"], testNamespace);
        var vector = initialFetch["update-vector-id-2"];
        vector.Values[0] = 23;
        await Fixture.Index.Update(vector, testNamespace);

        Vector updatedVector;
        var attemptCount = 0;
        do
        {
            await Task.Delay(TestFixture.DelayInterval);
            attemptCount++;
            var finalFetch = await Fixture.Index.Fetch(["update-vector-id-2"], testNamespace);
            updatedVector = finalFetch["update-vector-id-2"];
        } while (updatedVector.Values[0] != 23 && attemptCount < TestFixture.MaxAttemptCount);

        Assert.Equal("update-vector-id-2", updatedVector.Id);
        Assert.Equal([23, 3, 5, 7, 11, 13, 17, 19], updatedVector.Values);

        await Fixture.DeleteAndWait(["update-vector-id-1"], testNamespace);

        var stats = await Fixture.Index.DescribeStats();
        Assert.Equal((uint)2, stats.Namespaces.Where(x => x.Name == testNamespace).Select(x => x.VectorCount).SingleOrDefault());

        await Fixture.DeleteAndWait(["update-vector-id-2", "update-vector-id-3"], testNamespace);
    }

    [Fact]
    public async Task Upsert_on_existing_vector_makes_an_update()
    {
        var testNamespace = "upsert-on-existing";
        var newVectors = new Vector[]
            {
                new() { Id = "update-vector-id-1", Values = [1, 3, 5, 7, 9, 11, 13, 15] },
                new() { Id = "update-vector-id-2", Values = [2, 3, 5, 7, 11, 13, 17, 19] },
                new() { Id = "update-vector-id-3", Values = [2, 1, 3, 4, 7, 11, 18, 29] },
            };

        await Fixture.InsertAndWait(newVectors, testNamespace);

        var newExistingVector = new Vector() { Id = "update-vector-id-3", Values = [0, 1, 1, 2, 3, 5, 8, 13] };

        await Fixture.Index.Upsert([newExistingVector], testNamespace);

        Vector updatedVector;
        var attemptCount = 0;
        do
        {
            await Task.Delay(TestFixture.DelayInterval);
            attemptCount++;
            var finalFetch = await Fixture.Index.Fetch(["update-vector-id-3"], testNamespace);
            updatedVector = finalFetch["update-vector-id-3"];
        } while (updatedVector.Values[0] != 0 && attemptCount < TestFixture.MaxAttemptCount);

        Assert.Equal("update-vector-id-3", updatedVector.Id);
        Assert.Equal([0, 1, 1, 2, 3, 5, 8, 13], updatedVector.Values);
    }

    [Fact]
    public async Task Delete_all_vectors_from_namespace()
    {
        var testNamespace = "delete-all-namespace";
        var newVectors = new Vector[]
            {
                new() { Id = "delete-all-vector-id-1", Values = [1, 3, 5, 7, 9, 11, 13, 15] },
                new() { Id = "delete-all-vector-id-2", Values = [2, 3, 5, 7, 11, 13, 17, 19] },
                new() { Id = "delete-all-vector-id-3", Values = [2, 1, 3, 4, 7, 11, 18, 29] },
            };

        await Fixture.InsertAndWait(newVectors, testNamespace);

        await Fixture.Index.DeleteAll(testNamespace);

        IndexStats stats;
        var attemptCount = 0;
        do
        {
            await Task.Delay(TestFixture.DelayInterval);
            attemptCount++;
            stats = await Fixture.Index.DescribeStats();
        } while (stats.Namespaces.Where(x => x.Name == testNamespace).Select(x => x.VectorCount).SingleOrDefault() > 0 
            && attemptCount <= TestFixture.MaxAttemptCount);

        Assert.Equal((uint)0, stats.Namespaces.Where(x => x.Name == testNamespace).Select(x => x.VectorCount).SingleOrDefault());
    }

    [Fact]
    public async Task Delete_vector_that_doesnt_exist()
    {
        await Fixture.Index.Delete(["non-existing-index"]);
    }

    public class TestFixture : IAsyncLifetime
    {
        // 10s with 100ms intervals
        public const int MaxAttemptCount = 100;
        public const int DelayInterval = 100;
        private const string IndexName = "serverless-data-tests";

        public PineconeClient Pinecone { get; private set; } = null!;
        public Index<GrpcTransport> Index { get; private set; } = null!;

        public async Task InitializeAsync()
        {
            Pinecone = new PineconeClient(UserSecrets.Read("PineconeApiKey"));

            await ClearIndexesAsync();
            await CreateIndexAndWait();
            await AddSampleDataAsync();
        }

        private async Task CreateIndexAndWait()
        {
            var attemptCount = 0;
            await Pinecone.CreateServerlessIndexAsync(IndexName, dimiension: 8, metric: Metric.Euclidean, cloud: "aws", region: "us-east-1");

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

        private async Task AddSampleDataAsync()
        {
            var basicVectors = Enumerable.Range(1, 5).Select(i => new Vector
            {
                Id = "basic-vector-" + i,
                Values = [i * 0.5f, i * 1.0f, i * 1.5f, i * 2.0f, i * 2.5f, i * 3.0f, i * 3.5f, i * 4.0f],
            }).ToList();

            await InsertAndWait(basicVectors);

            var customNamespaceVectors = Enumerable.Range(1, 3).Select(i => new Vector
            {
                Id = "custom-namespace-vector-" + i,
                Values = [i * 1.1f, i * 2.2f, i * 3.3f, i * 4.4f, i * 5.5f, i * 6.6f, i * 7.7f, i * 8.8f],
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
                new() { Id = "metadata-vector-1", Values = [2, 3, 5, 7, 11, 13, 17, 19], Metadata = metadata1 },
                new() { Id = "metadata-vector-2", Values = [0, 1, 1, 2, 3, 5, 8, 13], Metadata = metadata2 },
                new() { Id = "metadata-vector-3", Values = [2, 1, 3, 4, 7, 11, 18, 29], Metadata = metadata3 },
            };

            await InsertAndWait(metadataVectors);
        }

        public async Task<uint> InsertAndWait(IEnumerable<Vector> vectors, string? indexNamespace = null)
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
            var stats = await Index.DescribeStats();
            var vectorCountBefore = stats.Namespaces.Single(x => x.Name == (indexNamespace ?? "")).VectorCount;

            var attemptCount = 0;
            await Index.Delete(ids, indexNamespace);
            long vectorCount;
            do
            {
                await Task.Delay(DelayInterval);
                attemptCount++;
                stats = await Index.DescribeStats();
                vectorCount = stats.Namespaces.Single(x => x.Name == (indexNamespace ?? "")).VectorCount;
            } while (vectorCount > vectorCountBefore - ids.Count() && attemptCount <= MaxAttemptCount);

            if (vectorCount > vectorCountBefore - ids.Count())
            {
                throw new InvalidOperationException("'Delete' operation didn't complete in time.");
            }
        }

        public async Task DisposeAsync()
        {
            await ClearIndexesAsync();
            Pinecone.Dispose();
        }

        private async Task ClearIndexesAsync()
        {
            foreach (var existingIndex in await Pinecone.ListIndexes())
            {
                await DeleteExistingIndexAndWaitAsync(existingIndex.Name);
            }
        }

        private async Task DeleteExistingIndexAndWaitAsync(string indexName)
        {
            var exists = true;
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

            if (exists)
            {
                throw new InvalidOperationException("'Delete index' operation didn't complete in time. Index name: " + indexName);
            }
        }
    }
}