# Pinecone.NET

Pinecone.NET is a fully-fledged C# library for the Pinecone vector database.  
This is a community library that provides first-class support for Pinecone in C# and F#.

## Features

- Standard operations on pod-based and serverless indexes
- gRPC and REST transports for vector operations
- Sparse-dense vectors
- Metadata support
- Client-side gRPC load balancing
- Efficient vector serialization
- NativeAOT compatibility (e.g. for AWS Lambda)
- Automatic batching and parallelization for upsert, fetch and delete operations
- Exceptions to save partial results and recover from failures on batched and streamed operations

## Installation

`dotnet add package Pinecone.NET` or `Install-Package Pinecone.NET`

## Usage
Working with indexes
```csharp
using Pinecone;

// Initialize the client with your API key
using var pinecone = new PineconeClient("your-api-key");

// List all indexes
var indexes = await pinecone.ListIndexes();

// Create a new index if it doesn't exist
var indexName = "myIndex";
if (!indexes.Contains(indexName))
{
    await pinecone.CreateServerlessIndex(indexName, 1536, Metric.Cosine, "aws", "us-east-1");
}

// Get the Pinecone index by name (uses gRPC by default).
// The index client is thread-safe, consider caching and/or
// injecting it as a singleton into your DI container.
using var index = await pinecone.GetIndex(indexName);

// Configure an index
await pinecone.ConfigureIndex(indexName, replicas: 2, podType: "p2");

// Delete an index
await pinecone.DeleteIndex(indexName);
```

Working with vectors
```csharp
// Assuming you have an instance of `index`
// Create and upsert vectors
var vectors = new[]
{
    new Vector
    {
        Id = "vector1",
        Values = new float[] { 0.1f, 0.2f, 0.3f },
        Metadata = new MetadataMap
        {
            ["genre"] = "horror",
            ["duration"] = 120
        }
    }
};
await index.Upsert(vectors);

// Fetch vectors by IDs
var fetched = await index.Fetch(["vector1"]);

// Query scored vectors by ID
var scored = await index.Query("vector1", topK: 10);

// Query scored vectors by a new, previously unseen vector
var vector = new[] { 0.1f, 0.2f, 0.3f, ... };
var scored = await index.Query(vector, topK: 10);

// Query scored vectors by ID with metadata filter
var filter = new MetadataMap
{
    ["genre"] = new MetadataMap
    {
        ["$in"] = new[] { "documentary", "action" }
    }
};
var scored = await index.Query("birds", topK: 10, filter);

// Delete vectors by vector IDs
await index.Delete(new[] { "vector1" });

// Delete vectors by metadata filter
await index.Delete(new MetadataMap
{
  ["genre"] = new MetadataMap
  {
     ["$in"] = new[] { "documentary", "action" }
  }
});

// Delete all vectors in the index
await index.DeleteAll();
```

Working with Collections
```csharp
using Pinecone;

// Assuming you have an instance of `PineconeClient` named `pinecone`
  
// List all collections
var collections = await pinecone.ListCollections();

// Create a new collection
await pinecone.CreateCollection("myCollection", "myIndex");

// Describe a collection
var details = await pinecone.DescribeCollection("myCollection");

// Delete a collection
await pinecone.DeleteCollection("myCollection");
```

## Contributing

Contributions are welcome! Feel free to open an issue or a PR.
