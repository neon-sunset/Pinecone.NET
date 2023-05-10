using Microsoft.SemanticKernel;
using Pinecone;

var oaiOrg = Environment.GetEnvironmentVariable("OAI_ORG");
var oaiKey = Environment.GetEnvironmentVariable("OAI_KEY")
    ?? throw new("OAI_KEY is not set");

var pineconeIndex = "playground";
var pineconeEnv = "us-east4-gcp";
var pineconeKey = Environment.GetEnvironmentVariable("PINECONE_KEY")
    ?? throw new("PINECONE_KEY is not set");

var kernel = new KernelBuilder().Build();
kernel.Config.AddOpenAITextEmbeddingGenerationService(
    "default",
    "text-embedding-ada-002",
    oaiKey,
    oaiOrg);

var generator = kernel.Config.TextEmbeddingGenerationServices.First().Value(kernel);
var embeddings = await generator.GenerateEmbeddingsAsync(new[] { "Example embeddings for \"Hello World!\"" });

using var pinecone = new PineconeClient(pineconeKey, pineconeEnv);

// Get the list of indexes
var indexes = await pinecone.ListIndexes();

// Create the index if it doesn't exist
if (!indexes.Contains(pineconeIndex))
{
    // If you have a free plan, you can only create one index
    // so if you already have another one, this call might fail
    await pinecone.CreateIndex(new()
    {
        Name = pineconeIndex,
        Dimension = 1536,
        Metric = Metric.Cosine
    });
}

// Get the index which uses the gRPC transport (default)
// or specify the transport type explicitly with GetIndex<TTransport>(...)
// where TTransport is either GrpcTransport or RestTransport from Pinecone.Grpc or .Rest
using var index = await pinecone.GetIndex(pineconeIndex);

Console.WriteLine(index);

// MetadataMap is a Dictionary<string, MetadataValue>
// Which is a strongly typed definition of Pinecone's metadata representation
var metadata = new MetadataMap
{
    ["number"] = 1337, // Numbers are normalized to fp64 as per Pinecone's spec
    ["boolean"] = true,
    ["string"] = "Hello Pinecone!"
};

Console.WriteLine(await index.DescribeStats(metadata));

// Upsert a range of vectors
var addedCount = await index.Upsert(new[]
{
    new Vector
    {
        Id = "helloworld",
        // Get the previously generated embeddings by SK's OAI integration
        // And write them to a float array we will pass to Pinecone
        Values = embeddings[0].AsReadOnlySpan().ToArray(),
        Metadata = new()
        {
            ["kind"] = "test",
            ["conditional"] = true,
            ["seqstr"] = new MetadataValue[] { "one", "two", "three" }
        }
    }
});

// Fetch the vector we just added
var fetched = await index.Fetch(new[] { "helloworld" });
foreach (var kvp in fetched)
{
    Console.WriteLine(kvp);
}

// Query the index for the vector we just added and include the metadata (false by default)
// The result is a list of scored vectors with their values included (true by default)
var results = await index.Query("helloworld", topK: 10, includeMetadata: true);
foreach (var vector in results)
{
    Console.WriteLine(vector);
}

// Delete the range of vectors
await index.Delete(new[] { "helloworld" });
