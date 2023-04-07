using Pinecone;

var indexName = new PineconeIndexName("new-test-index");

using var pinecone = new PineconeClient("[REDACTED]", "[REDACTED]");

var index = (await pinecone.ListIndexes()).Contains(indexName)
    ? await pinecone.GetIndex(indexName)
    : await pinecone.CreateIndex(new()
    {
        Dimension = 512,
        Metric = PineconeMetric.Cosine,
        Name = indexName,
        Pods = 1,
        PodType = "s1.x1",
        Replicas = 1
    });

Console.WriteLine();