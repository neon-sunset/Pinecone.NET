﻿#nowarn "3391"
open Pinecone
open System.Collections.Generic

let createMetadata x =
    MetadataMap(x |> Seq.map (fun (k, m) -> KeyValuePair(k,m) ))

let main = task {
    use pinecone = new PineconeClient("[api-key]")

    // Check if the index exists and create it if it doesn't
    // Depending on the storage type and infrastructure state this may take a while
    // Free tier is limited to 1 pod index and 5 serverless indexes only
    let indexName = "test-index"
    let! indexList = pinecone.ListIndexes()
    if not (indexList |> Array.exists (fun index -> index.Name = indexName)) then
        // free serverless indexes are currently only available on AWS us-east-1
        do! pinecone.CreateServerlessIndex(indexName, 1536u, Metric.Cosine, "aws", "us-east-1")

    // Get the Pinecone index by name (uses gRPC by default).
    // The index client is thread-safe, consider caching and/or
    // injecting it as a singleton into your DI container.
    use! index = pinecone.GetIndex(indexName)

    let tags = [|"tag1" ; "tag2"|]
    let first = Vector(Id = "first", Values = Array.zeroCreate 1536, Metadata = createMetadata["new", true; "price", 50; "tags", tags])
    let second = Vector(Id = "second", Values = Array.zeroCreate 1536, Metadata = createMetadata["price", 50])
    
    // Upsert vectors into the index
    let! _ = index.Upsert [|first; second|]

    // Partially update a vector (allows to update dense/sparse/metadata properties only)
    do! index.Update("second", metadata = createMetadata["price", 99])

    // Specify metadata filter to query the index with
    let priceRange = createMetadata["price", createMetadata["$gte", 75; "$lte", 125]]

    // Query the index by embedding and metadata filter
    let! results = index.Query((Array.zeroCreate 1536), 3u, filter = priceRange, includeMetadata = true)
    let metadata =
        results
        |> Seq.collect _.Metadata
        |> Seq.map string
        |> String.concat "\n"
    printfn "%s" metadata

    // Remove the example vectors we just added
    do! index.Delete ["first"; "second"]
}

main.Wait()
