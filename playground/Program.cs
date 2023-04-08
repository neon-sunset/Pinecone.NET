using Pinecone;
using Pinecone.Transport;

var indexName = new PineconeIndexName("new-test-index");

using var pinecone = new PineconeClient("[REDACTED]", "[REDACTED]");
using var index = await pinecone.GetIndex<GrpcTransport>(indexName);

Console.WriteLine(await index.DescribeStats());