# Pinecone.NET

Pinecone.NET is a C# library for the Pinecone vector database. It aims to provide identical functionality to the official Python and Rust libraries.

## Features

- Standard Index operations
- gRPC and REST transports for vector operations
- Sparse-dense vectors
- Efficient vector serialization
- Metadata support
- NativeAOT compatibility

## Installation

`dotnet add package Pinecone.NET` or `Install-Package Pinecone.NET`

## Usage
Working with indexes
```csharp
using Pinecone;

// Initialize the client with your API key and environment
var apiKey = "<your-api-key>";
var environment = "<your-environment>"; // for example us-east4-gcp

using var pinecone = new PineconeClient(apiKey, environment);

// List all indexes
var indexes = await pinecone.ListIndexes();

// Create a new index if it doesn't exist
var indexName = "myIndex";
if (!indexes.Contains(indexName))
{
    await pinecone.CreateIndex(indexName, 1536, Metric.Cosine);
}

// Get an index (uses gRPC transport by default)
using var index = await pinecone.GetIndex(indexName);

// Get an index that uses specific transport
using Pinecone.Rest;
...
using var index = await pinecone.GetIndex<RestTransport>(indexName);

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
var fetched = await index.Fetch(new[] { "vector1" });

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
        ["$in"] = new MetadataValue[] { "documentary", "action" }
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
     ["$in"] = new MetadataValue[] { "documentary", "action" }
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

Contributions are welcome! Feel free open an issue or a PR.
