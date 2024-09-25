using Microsoft.Extensions.Logging;
using Pinecone;
using PineconeTests.Xunit;
using Xunit;
using Xunit.Sdk;

namespace PineconeTests;

public abstract class DataTestBase<TFixture, TTransport>(TFixture fixture) : IClassFixture<TFixture>
    where TFixture: DataTestFixtureBase<TTransport>
    where TTransport: ITransport<TTransport>
{
    protected TFixture Fixture { get; } = fixture;

    [PineconeFact]
    public async Task Basic_query()
    {
        var x = 0.314f;

        var results = await Fixture.Index.Query(
            new[] { x * 0.1f, x * 0.2f, x * 0.3f, x * 0.4f, x * 0.5f, x * 0.6f, x * 0.7f, x * 0.8f },
            topK: 20);

        Assert.Equal(10, results.Length);

        results =
            await Fixture.Index.Query(
                new[] { 0.7f, 7.7f, 77.7f, 777.7f, 7777.7f, 77777.7f, 777777.7f, 7777777.7f },
                topK: 10, 
                indexNamespace: "namespace1");

        Assert.Equal(3, results.Length);
    }

    [PineconeFact]
    public async Task Query_by_Id()
    {
        var result = await Fixture.Index.Query("basic-vector-3", topK: 2);

        // NOTE: query by id uses Approximate Nearest Neighbor, which doesn't guarantee the input vector
        // to appear in the results, so we just check the result count
        Assert.Equal(2, result.Length);
    }

    [PineconeFact]
    public async Task Query_with_basic_metadata_filter()
    {
        var filter = new MetadataMap
        {
            ["type"] = "number set"
        };

        var result = await Fixture.Index.Query(new[] { 3f, 4, 5, 6, 7, 8, 9, 10 }, topK: 5, filter);

        Assert.Equal(3, result.Length);
        var ordered = result.OrderBy(x => x.Id).ToList();

        Assert.Equal("metadata-vector-1", ordered[0].Id);
        Assert.Equal(new[] { 2f, 3, 5, 7, 11, 13, 17, 19 }, ordered[0].Values!.Value);
        Assert.Equal("metadata-vector-2", ordered[1].Id);
        Assert.Equal(new[] { 0f, 1, 1, 2, 3, 5, 8, 13 }, ordered[1].Values!.Value);
        Assert.Equal("metadata-vector-3", ordered[2].Id);
        Assert.Equal(new[] { 2f, 1, 3, 4, 7, 11, 18, 29 }, ordered[2].Values!.Value);
    }

    [PineconeFact]
    public async Task Query_include_metadata_in_result()
    {
        var filter = new MetadataMap
        {
            ["subtype"] = "fibo"
        };

        var result = await Fixture.Index.Query(new[] { 3f, 4, 5, 6, 7, 8, 9, 10 }, topK: 5, filter, includeMetadata: true);

        Assert.Single(result);
        Assert.Equal("metadata-vector-2", result[0].Id);
        Assert.Equal(new[] { 0f, 1, 1, 2, 3, 5, 8, 13 }, result[0].Values!.Value);
        var metadata = result[0].Metadata;
        Assert.NotNull(metadata);

        Assert.Equal("number set",metadata["type"]);
        Assert.Equal("fibo", metadata["subtype"]);

        var innerList = (MetadataValue[])metadata["list"].Inner!;
        Assert.Equal("0", innerList[0]);
        Assert.Equal("1", innerList[1]);
    }

    [PineconeFact]
    public async Task Query_with_metadata_filter_composite()
    {
        var filter = new MetadataMap
        {
            ["type"] = "number set",
            ["overhyped"] = false
        };

        var result = await Fixture.Index.Query(new[] { 3f, 4, 5, 6, 7, 8, 9, 10 }, topK: 5, filter);

        Assert.Equal(2, result.Length);
        var ordered = result.OrderBy(x => x.Id).ToList();

        Assert.Equal("metadata-vector-1", ordered[0].Id);
        Assert.Equal(new[] { 2f, 3, 5, 7, 11, 13, 17, 19 }, ordered[0].Values!.Value);
        Assert.Equal("metadata-vector-3", ordered[1].Id);
        Assert.Equal(new[] { 2f, 1, 3, 4, 7, 11, 18, 29 }, ordered[1].Values!.Value);
    }

    [PineconeFact]
    public async Task Query_with_metadata_list_contains()
    {
        var filter = new MetadataMap
        {
            ["rank"] = new MetadataMap() { ["$in"] = new int[] { 12, 3 } }
        };

        var result = await Fixture.Index.Query(new[] { 3f, 4, 5, 6, 7, 8, 9, 10 }, topK: 10, filter, includeMetadata: true);

        Assert.Equal(2, result.Length);
        var ordered = result.OrderBy(x => x.Id).ToList();

        Assert.Equal("metadata-vector-1", ordered[0].Id);
        Assert.Equal(new[] { 2f, 3, 5, 7, 11, 13, 17, 19 }, ordered[0].Values!.Value);
        Assert.Equal("metadata-vector-3", ordered[1].Id);
        Assert.Equal(new[] { 2f, 1, 3, 4, 7, 11, 18, 29 }, ordered[1].Values!.Value);
    }

    [PineconeFact]
    public async Task Query_with_metadata_complex()
    {
        var filter = new MetadataMap
        {
            ["$or"] = new List<MetadataValue> 
            { 
                new MetadataMap() { ["rank"] = new MetadataMap() { ["$gt"] = 10 } }, 
                new MetadataMap() 
                { 
                    ["$and"] = new List<MetadataValue>
                    {
                        new MetadataMap() { ["subtype"] = "primes" },
                        new MetadataMap() { ["overhyped"] = false }
                    }
                } 
            }
        };

        var result = await Fixture.Index.Query(new[] { 3f, 4, 5, 6, 7, 8, 9, 10 }, topK: 10, filter, includeMetadata: true);

        Assert.Equal(2, result.Length);
        var ordered = result.OrderBy(x => x.Id).ToList();

        Assert.Equal("metadata-vector-1", ordered[0].Id);
        Assert.Equal(new[] { 2f, 3, 5, 7, 11, 13, 17, 19 }, ordered[0].Values!.Value);
        Assert.Equal("metadata-vector-3", ordered[1].Id);
        Assert.Equal(new[] { 2f, 1, 3, 4, 7, 11, 18, 29 }, ordered[1].Values!.Value);
    }

    [PineconeFact]
    public async Task Basic_fetch()
    {
        var results = await Fixture.Index.Fetch(["basic-vector-1", "basic-vector-3"]);
        var orderedResults = results.OrderBy(x => x.Key).ToList();
        
        Assert.Equal(2, orderedResults.Count);

        Assert.Equal("basic-vector-1", orderedResults[0].Key);
        Assert.Equal("basic-vector-1", orderedResults[0].Value.Id);
        Assert.Equal(new[] { 0.5f, 1.0f, 1.5f, 2.0f, 2.5f, 3.0f, 3.5f, 4.0f }, orderedResults[0].Value.Values);

        Assert.Equal("basic-vector-3", orderedResults[1].Key);
        Assert.Equal("basic-vector-3", orderedResults[1].Value.Id);
        Assert.Equal(new[] { 1.5f, 3.0f, 4.5f, 6.0f, 7.5f, 9.0f, 10.5f, 12.0f }, orderedResults[1].Value.Values);
    }

    [PineconeFact]
    public async Task Fetch_sparse_vector()
    {
        var results = await Fixture.Index.Fetch(["sparse-1"]);

        Assert.Single(results);
        Assert.True(results.ContainsKey("sparse-1"));
        var resultVector = results["sparse-1"];

        Assert.Equal("sparse-1", resultVector.Id);
        Assert.Equal(new[] { 5f, 10, 15, 20, 25, 30, 35, 40 }, resultVector.Values);
        Assert.NotNull(resultVector.SparseValues);
        Assert.Equal(new[] { 1u, 4u }, resultVector.SparseValues.Value.Indices);
        Assert.Equal(new[] { 0.2f, 0.5f }, resultVector.SparseValues.Value.Values);
    }

    [PineconeFact]
    public async Task Basic_vector_upsert_update_delete()
    {
        var testNamespace = "upsert-update-delete-namespace";
        var newVectors = new Vector[]
            {
                new() { Id = "update-vector-id-1", Values = new[] { 1f, 3, 5, 7, 9, 11, 13, 15 } },
                new() { Id = "update-vector-id-2", Values = new[] { 2f, 3, 5, 7, 11, 13, 17, 19 } },
                new() { Id = "update-vector-id-3", Values = new[] { 2f, 1, 3, 4, 7, 11, 18, 29 } },
            };

        await Fixture.InsertAndWait(newVectors, testNamespace);

        var initialFetch = await Fixture.Index.Fetch(["update-vector-id-2"], testNamespace);
        var vector = initialFetch["update-vector-id-2"];
        var values = vector.Values.ToArray();
        values[0] = 23;
        await Fixture.Index.Update(vector with { Values = values }, testNamespace);

        Vector updatedVector;
        var attemptCount = 0;
        do
        {
            await Task.Delay(DataTestFixtureBase<TTransport>.DelayInterval);
            attemptCount++;
            var finalFetch = await Fixture.Index.Fetch(["update-vector-id-2"], testNamespace);
            updatedVector = finalFetch["update-vector-id-2"];
        } while (updatedVector.Values.Span[0] != 23 && attemptCount < DataTestFixtureBase<TTransport>.MaxAttemptCount);

        Assert.Equal("update-vector-id-2", updatedVector.Id);
        Assert.Equal(new[] { 23f, 3, 5, 7, 11, 13, 17, 19 }, updatedVector.Values);

        await Fixture.DeleteAndWait(["update-vector-id-1"], testNamespace);

        var stats = await Fixture.Index.DescribeStats();
        Assert.Equal((uint)2, stats.Namespaces.Where(x => x.Name == testNamespace).Select(x => x.VectorCount).SingleOrDefault());

        await Fixture.DeleteAndWait(["update-vector-id-2", "update-vector-id-3"], testNamespace);
    }

    [PineconeFact]
    public async Task Upsert_on_existing_vector_makes_an_update()
    {
        var testNamespace = "upsert-on-existing";
        var newVectors = new Vector[]
            {
                new() { Id = "update-vector-id-1", Values = new[] { 1f, 3, 5, 7, 9, 11, 13, 15 } },
                new() { Id = "update-vector-id-2", Values = new[] { 2f, 3, 5, 7, 11, 13, 17, 19 } },
                new() { Id = "update-vector-id-3", Values = new[] { 2f, 1, 3, 4, 7, 11, 18, 29 } },
            };

        await Fixture.InsertAndWait(newVectors, testNamespace);

        var newExistingVector = new Vector() { Id = "update-vector-id-3", Values = new[] { 0f, 1, 1, 2, 3, 5, 8, 13 } };

        await Fixture.Index.Upsert([newExistingVector], testNamespace);

        Vector updatedVector;
        var attemptCount = 0;
        do
        {
            await Task.Delay(DataTestFixtureBase<TTransport>.DelayInterval);
            attemptCount++;
            var finalFetch = await Fixture.Index.Fetch(["update-vector-id-3"], testNamespace);
            updatedVector = finalFetch["update-vector-id-3"];
        } while (updatedVector.Values.Span[0] != 0 && attemptCount < DataTestFixtureBase<TTransport>.MaxAttemptCount);

        Assert.Equal("update-vector-id-3", updatedVector.Id);
        Assert.Equal(new[] { 0f, 1, 1, 2, 3, 5, 8, 13 }, updatedVector.Values);
    }

    [PineconeFact]
    public async Task Delete_all_vectors_from_namespace()
    {
        var testNamespace = "delete-all-namespace";
        var newVectors = new Vector[]
            {
                new() { Id = "delete-all-vector-id-1", Values = new[] { 1f, 3, 5, 7, 9, 11, 13, 15 } },
                new() { Id = "delete-all-vector-id-2", Values = new[] { 2f, 3, 5, 7, 11, 13, 17, 19 } },
                new() { Id = "delete-all-vector-id-3", Values = new[] { 2f, 1, 3, 4, 7, 11, 18, 29 } },
            };

        await Fixture.InsertAndWait(newVectors, testNamespace);

        await Fixture.Index.DeleteAll(testNamespace);

        IndexStats stats;
        var attemptCount = 0;
        do
        {
            await Task.Delay(DataTestFixtureBase<TTransport>.DelayInterval);
            attemptCount++;
            stats = await Fixture.Index.DescribeStats();
        } while (stats.Namespaces.Where(x => x.Name == testNamespace).Select(x => x.VectorCount).SingleOrDefault() > 0 
            && attemptCount <= DataTestFixtureBase<TTransport>.MaxAttemptCount);

        Assert.Equal((uint)0, stats.Namespaces.Where(x => x.Name == testNamespace).Select(x => x.VectorCount).SingleOrDefault());
    }

    [PineconeFact]
    public async Task Delete_vector_that_doesnt_exist()
    {
        await Fixture.Index.Delete(["non-existing-vector"]);
    }

    [PineconeFact(Skip = "Logging changes WIP")]
    public async Task Logging_is_properly_wired()
    {
        var logOutput = new List<string>();
        var loggingClient = new PineconeClient(UserSecretsExtensions.ReadPineconeApiKey(), new MyLoggerFactory(logOutput));
        var loggingIndex = await loggingClient.GetIndex(Fixture.IndexName);

        await loggingClient.ListIndexes();
        Assert.Contains($"[Pinecone.PineconeClient | Trace]: List indexes started.", logOutput);
        Assert.Contains(logOutput, x => x.StartsWith("[Pinecone.PineconeClient | Debug]: List indexes completed - indexes found: "));

        await loggingClient.ListCollections();
        Assert.Contains($"[Pinecone.PineconeClient | Trace]: List collections started.", logOutput);
        Assert.Contains(logOutput, x => x.StartsWith("[Pinecone.PineconeClient | Debug]: List collections completed - collections found: "));

        await loggingIndex.Query(new[] { 0.1f, 0.2f, 0.3f, 0.4f, 0.5f, 0.6f, 0.7f, 0.8f }, topK: 2);
        Assert.Contains($"[Pinecone.Index | Trace]: Query index '{Fixture.IndexName}' based on vector values started.", logOutput);
        Assert.Contains($"[Pinecone.Index | Debug]: Query index '{Fixture.IndexName}' based on vector values completed.", logOutput);

        await loggingIndex.Query("basic-vector-1", topK: 2);
        Assert.Contains($"[Pinecone.Index | Trace]: Query index '{Fixture.IndexName}' based on vector ID started.", logOutput);
        Assert.Contains($"[Pinecone.Index | Debug]: Query index '{Fixture.IndexName}' based on vector ID completed.", logOutput);

        var vectors = await loggingIndex.Fetch(["basic-vector-2"]);
        Assert.Contains($"[Pinecone.Index | Trace]: Fetch from index '{Fixture.IndexName}' started.", logOutput);
        Assert.Contains($"[Pinecone.Index | Debug]: Fetch from index '{Fixture.IndexName}' completed.", logOutput);

        // "upserting" the same vector to avoid side-effects
        await loggingIndex.Upsert([vectors["basic-vector-2"]]);
        Assert.Contains($"[Pinecone.Index | Trace]: Upsert to index '{Fixture.IndexName}' started.", logOutput);
        Assert.Contains($"[Pinecone.Index | Debug]: Upsert to index '{Fixture.IndexName}' completed - upserted count: 1.", logOutput);

        await loggingIndex.Delete(["non-existing-vector"]);
        Assert.Contains($"[Pinecone.Index | Trace]: Delete from index '{Fixture.IndexName}' based on IDs started.", logOutput);
        Assert.Contains($"[Pinecone.Index | Debug]: Delete from index '{Fixture.IndexName}' based on IDs completed.", logOutput);

        // error from PineconeClient
        var message = (await Assert.ThrowsAsync<ArgumentException>(() => loggingClient.ConfigureIndex(Fixture.IndexName))).Message;
        Assert.Equal("At least one of the following parameters must be specified: replicas, podType.", message);
        Assert.Contains($"[Pinecone.PineconeClient | Trace]: Configure index '{Fixture.IndexName}' started.", logOutput);
        Assert.Contains($"[Pinecone.PineconeClient | Error]: Configure index '{Fixture.IndexName}' failed: {message}", logOutput);

        // error from Transport layer
        message = (await Assert.ThrowsAsync<ArgumentException>(() => loggingIndex.Query(id: null!, topK: 2))).Message;
        Assert.Equal("At least one of the following parameters must be non-null: id, values, sparseValues.", message);
        Assert.Contains($"[Pinecone.Index | Trace]: Query index '{Fixture.IndexName}' based on vector ID started.", logOutput);
        Assert.Contains($"[Pinecone.Grpc.GrpcTransport | Error]: Query failed: {message}", logOutput);

        // verify that Grpc and HttpClient loggers are correctly wired up
        Assert.Contains(logOutput, x => x.StartsWith("[Grpc.Net.Client.Internal.GrpcCall |"));
        Assert.Contains(logOutput, x => x.StartsWith("[System.Net.Http.HttpClient |"));
    }

    private class MyLoggerFactory(IList<string> output) : ILoggerFactory
    {
        public void AddProvider(ILoggerProvider provider) { }

        public ILogger CreateLogger(string categoryName)
            => new MyLogger(output, categoryName);

        public void Dispose() { }
    }

    internal class MyLogger(IList<string> output, string categoryName) : ILogger
    {
        public IDisposable BeginScope<TState>(TState state)
            => null!;

        public bool IsEnabled(LogLevel logLevel)
            => logLevel != LogLevel.Trace;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (IsEnabled(logLevel))
            {
                var message = "[" + categoryName + " | " + logLevel + "]: " + formatter(state, exception).Trim();
                output.Add(message);
            }
        }
    }
}