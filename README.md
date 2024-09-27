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

// Get the Pinecone index by name (uses REST by default).
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
        Values = new float[] { 0.1f, 0.2f, 0.3f, ... },
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

## Advanced
Recovering from failures on batched parallel upsert
```csharp
// Upsert with recovery from up to three failures on batched parallel upsert.
//
// The parallelization is done automatically by the client based on the vector
// dimension and the number of vectors to upsert. It aims to keep the individual
// request size below Pinecone's 2MiB limit with some safety margin for metadata.
// This behavior can be further controlled by calling the 'Upsert' overload with
// custom values for 'batchSize' and 'parallelism' parameters.
//
// This is not the most efficient implementation in terms of allocations in
// GC pause frequency sensitive scenarios, but is perfectly acceptable
// for pretty much all regular back-end applications.

// Assuming there is an instance of 'index' available
// Generate 25k random vectors
var vectors = Enumerable
    .Range(0, 25_000)
    .Select(_ => new Vector
    {
        Id = Guid.NewGuid().ToString(),
        Values = Enumerable
            .Range(0, 1536)
            .Select(_ => Random.Shared.NextSingle())
            .ToArray()
    })
    .ToArray();

// Specify the retry limit we are okay with
var retries = 3;
do
{
    try
    {
        // Perform the upsert
        await index.Upsert(vectors);
        // If no exception is thrown, break out of the retry loop
        break;
    }
    catch (ParallelUpsertException e) when (retries-- > 0)
    {
        // Create a hash set to efficiently filter out the failed vectors
        var filter = e.FailedBatchVectorIds.ToHashSet();
        // Filter out the failed vectors from the batch and assign them to
        // the 'vectors' variable consumed by 'Upsert' operation above
        vectors = vectors.Where(v => filter.Contains(v.Id)).ToArray();
        Console.WriteLine($"Retrying upsert due to error: {e.Message}");
    }
} while (retries > 0);
```

A similar approach can be used to recover from other streamed or batched operations.  
See `ListOperationException`, `ParallelFetchException` and `ParallelDeleteException` in [VectorTypes.cs](src/Types/VectorTypes.cs#L192-L282).

## REST vs gRPC transport

Prefer `RestTransport` by default. Please find the detailed explanation for specific scenarios below.

### Low-throughput bandwith minimization

`GrpcTransport` is a viable alternative for reducing network traffic when working with large vectors under low to moderate throughput scenarios.  
Protobuf encodes vectors in a much more compact manner, so if you have high-dimensional vectors (1536-3072+), low degree of request concurrency and usually upsert or fetch vectors in small bacthes, it is worth considering `GrpcTransport` for your use case.

Theoretically, low concurrency throughput may be higher with `GrpcTransport` due to the reduced network traffic, but because how trivial it is to simply
dispatch multiple requests in parallel (and the fact that `Fetch`, `Upsert` and `Delete` do so automatically), the advantages of this approach are likely to be limited.

### High concurrency querying, high-throughput vector fetching and upserting

At the time of writing, Pinecone's HTTP/2 stack is configured to allow few or even just 1 concurrent stream per single HTTP/2 connection.  
Because HTTP/2 is mandatory for gRPC, this causes significant request queuing over a gRPC channel under high concurrency scenarios, resulting in a poor scalability of the `GrpcTransport` with low ceiling for throughput.

The users that are not aware of this limitation may experience unexpected latency increase in their query operations under growing user count per application node and/or much lower than expected upsert and fetch throughput even when manually specifying greater degree of parallelism of `Upsert` and `Fetch` operations.

`Pinecone.NET` partially mitigates this issue by configuring gRPC channel to take advantage of client-side load balancing (DNS records based) to make use of multiple subchannels (there are currently 3 as returned by DNS query), which is expected to provide better throughput than other clients still. It also enables the use of multiple HTTP/2 connections per endpoint for the underlying `SocketsHttpHandler` but current gRPC implementation does not seem to properly take advantage of this.

The above is an observation of the client behavior under load-testing and additional infrastructure factors might be at play as indicated by user reports at Pinecone's community forum w.r.t. scalability in other implementations.

Expert users that still wish to use gRPC transport in high-load scenarios may want to explore further action items that are out of scope of this simple community-supported library:

- Implementing a custom subchannel balancer that forces per-connection scaling to match maximum request concurrency
- Investigating _potential_ Pinecone-side client throttling mechanism that prevents efficient pooling of multiple gRPC channels
- Implementing a simplified but completely custom gRPC transport that uses its own connection, channel and subchannel management, completely bypassing existing gRPC implementation

Regular users are advised to use `RestTransport` for high-throughput and/or high concurrency scenarios instead, unless their evaluation of `GrpcTransport` in their specific environment produces better results.

### Reducing application memory usage and GC pressure

As of right now, `RestTransport` and `System.Text.Json` it uses for serialization appear to be more memory-efficient than `GrpcTransport` and `Protobuf`. This is not an inherent limitation of gRPC, but rather a result of the current implementation of `grpc-dotnet`. `System.Text.Json` is heavily optimized with regards to allocation traffic and results in significantly lower sustained memory usage under both light and heavy load. Given the current state of `grpc-dotnet` implementation, I do not anticipate this to change in the near future. It is "good enough" for most applications, but the sustained heap size difference under load is significant enough to warrant stating this explicitly.

Please note that `Pinecone.NET` already performs zero-copy construction/reading of `RepeatedField<float>` that store vector values to alleviate the allocation pressure, but it is not enough to offset the advantage of using plain `System.Text.Json` serialization.

## Contributing

Contributions are welcome! Feel free to open an issue or a PR.
