using Pinecone;
using PineconeTests.Xunit;
using Xunit;

namespace PineconeTests;

public abstract class DataTestBase<TFixture>(TFixture fixture) : IClassFixture<TFixture>
    where TFixture: DataTestFixtureBase
{
    private TFixture Fixture { get; } = fixture;

    [PineconeFact]
    public async Task Basic_query()
    {
        var x = 0.314f;

        var results = await Fixture.Index.Query(
            [x * 0.1f, x * 0.2f, x * 0.3f, x * 0.4f, x * 0.5f, x * 0.6f, x * 0.7f, x * 0.8f],
            topK: 20);

        Assert.Equal(10, results.Length);

        results =
            await Fixture.Index.Query(
                [0.7f, 7.7f, 77.7f, 777.7f, 7777.7f, 77777.7f, 777777.7f, 7777777.7f],
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

    [PineconeFact]
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

    [PineconeFact]
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

    [PineconeFact]
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

    [PineconeFact]
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

    [PineconeFact]
    public async Task Fetch_sparse_vector()
    {
        var results = await Fixture.Index.Fetch(["sparse-1"]);

        Assert.Single(results);
        Assert.True(results.ContainsKey("sparse-1"));
        var resultVector = results["sparse-1"];

        Assert.Equal("sparse-1", resultVector.Id);
        Assert.Equal([5, 10, 15, 20, 25, 30, 35, 40], resultVector.Values);
        Assert.NotNull(resultVector.SparseValues);
        Assert.Equal([1, 4], resultVector.SparseValues.Value.Indices);
        Assert.Equal([0.2f, 0.5f], resultVector.SparseValues.Value.Values);
    }

    [PineconeFact]
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
            await Task.Delay(DataTestFixtureBase.DelayInterval);
            attemptCount++;
            var finalFetch = await Fixture.Index.Fetch(["update-vector-id-2"], testNamespace);
            updatedVector = finalFetch["update-vector-id-2"];
        } while (updatedVector.Values[0] != 23 && attemptCount < DataTestFixtureBase.MaxAttemptCount);

        Assert.Equal("update-vector-id-2", updatedVector.Id);
        Assert.Equal([23, 3, 5, 7, 11, 13, 17, 19], updatedVector.Values);

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
            await Task.Delay(DataTestFixtureBase.DelayInterval);
            attemptCount++;
            var finalFetch = await Fixture.Index.Fetch(["update-vector-id-3"], testNamespace);
            updatedVector = finalFetch["update-vector-id-3"];
        } while (updatedVector.Values[0] != 0 && attemptCount < DataTestFixtureBase.MaxAttemptCount);

        Assert.Equal("update-vector-id-3", updatedVector.Id);
        Assert.Equal([0, 1, 1, 2, 3, 5, 8, 13], updatedVector.Values);
    }

    [PineconeFact]
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
            await Task.Delay(DataTestFixtureBase.DelayInterval);
            attemptCount++;
            stats = await Fixture.Index.DescribeStats();
        } while (stats.Namespaces.Where(x => x.Name == testNamespace).Select(x => x.VectorCount).SingleOrDefault() > 0 
            && attemptCount <= DataTestFixtureBase.MaxAttemptCount);

        Assert.Equal((uint)0, stats.Namespaces.Where(x => x.Name == testNamespace).Select(x => x.VectorCount).SingleOrDefault());
    }

    [PineconeFact]
    public async Task Delete_vector_that_doesnt_exist()
    {
        await Fixture.Index.Delete(["non-existing-index"]);
    }
}