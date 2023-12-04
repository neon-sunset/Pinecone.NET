open Pinecone
open System.Collections.Generic

let createMetadata x =
    MetadataMap(x |> Seq.map (fun (k, m) -> KeyValuePair(k,m) ))

let main = task {
    use pinecone = new PineconeClient("[api-key]", "[pinecone-env]")

    // Check if the index exists and create it if it doesn't
    // Depending on the storage type and infrastructure state this may take a while
    // Free tier is limited to 1 index only
    let indexName = "test-index"
    let! indexList = pinecone.ListIndexes()
    if not (indexList |> Array.contains indexName) then
        do! pinecone.CreateIndex(indexName, 1536u, Metric.Cosine)
    
    // Get the Pinecone index by name (uses gRPC by default).
    // The index client is thread-safe, consider caching and/or
    // injecting it as a singleton into your DI container.
    use! index = pinecone.GetIndex(indexName)

    let tags = [|"tag1" ; "tag2"|]
    let first: Vector = Vector(Id = "first", Values = Array.zeroCreate 1536, Metadata = createMetadata["new", true; "price", 50; "tags", tags])
    let second: Vector = Vector(Id = "second", Values = Array.zeroCreate 1536, Metadata = createMetadata["price", 50])
    
    // Upsert vectors into the index
    let! _ = index.Upsert [|first; second|]

    // Specify metadata filter to query the index with
    let priceRange = createMetadata["price", createMetadata["$gte", 75; "$lte", 125]]

    // Partially update a vector (allows to update dense/sparse/metadata properties only)
    do! index.Update("second", metadata = createMetadata["price", 99])

    // Query the index by embedding and metadata filter
    let! results = index.Query((Array.zeroCreate<float32> 1536), 3u, filter = priceRange, includeMetadata = true)
    let tt = results |> Seq.collect (fun v -> v.Metadata)
    printfn "%s" (results |> Seq.collect (fun v -> v.Metadata) |> Seq.map string |> String.concat "\n")

    // Remove the example vectors we just added
    do! index.Delete(["first"; "second"])
}

main.Wait()
