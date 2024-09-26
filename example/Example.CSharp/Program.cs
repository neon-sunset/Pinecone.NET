using Pinecone;

using var pinecone = new PineconeClient("[api-key]");

// Check if the index exists and create it if it doesn't
// Depending on the storage type and infrastructure state this may take a while
// Free tier is limited to 1 pod index and 5 serverless indexes only
var indexName = "test-index";
var indexList = await pinecone.ListIndexes();

if (!indexList.Select(x => x.Name).Contains(indexName))
{
    // free serverless indexes are currently only available on AWS us-east-1
    await pinecone.CreateServerlessIndex(indexName, 1536, Metric.Cosine, "aws", "us-east-1");
}

// Get the Pinecone index by name (uses gRPC by default).
// The index client is thread-safe, consider caching and/or
// injecting it as a singleton into your DI container.
using var index = await pinecone.GetIndex(indexName);

// Define a helper method to generate random vectors,
// Pinecone disallows vectors with all zeros.
static float[] GetRandomVector(int dimension) =>
    Enumerable
        .Range(0, dimension)
        .Select(_ => Random.Shared.NextSingle())
        .ToArray();

var first = new Vector
{
    Id = "first",
    Values = GetRandomVector(1536),
    Metadata = new()
    {
        ["new"] = true,
        ["price"] = 50,
        ["tags"] = new string[] { "tag1", "tag2" }
    }
};

var second = new Vector
{
    Id = "second",
    Values = GetRandomVector(1536),
    Metadata = new() { ["price"] = 100 }
};

// Upsert vectors into the index
await index.Upsert([first, second]);

// Partially update a vector (allows to update dense/sparse/metadata properties only)
await index.Update("second", metadata: new() { ["price"] = 99 });

// Specify metadata filter to query the index with
var priceRange = new MetadataMap
{
    ["price"] = new MetadataMap
    {
        ["$gte"] = 75,
        ["$lte"] = 125
    }
};

// Wait a bit for the index to update
await Task.Delay(1000);

// Query the index by embedding and metadata filter
var results = await index.Query(
    GetRandomVector(1536),
    topK: 3,
    filter: priceRange,
    includeMetadata: true);

Console.WriteLine(string.Join('\n', results.SelectMany(v => v.Metadata!)));

// List all vectors IDs in the index
await foreach (var id in index.List())
{
    Console.WriteLine(id);
}

// Remove the example vectors we just added
await index.Delete(["first", "second"]);
