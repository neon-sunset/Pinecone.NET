namespace Pinecone;

public record PineconeVector
{
    public required string Id { get; init; }

    public required float[] Value { get; init; }

    public SparseValues? SparseValues { get; init; }

    public MetadataValue? Metadata { get; init; }
}

public readonly record struct SparseValues
{
    public required uint[] Indices { get; init; }

    public required float[] Values { get; init; }
}

public record ScoredVector
{
    public required string Id { get; init; }

    public required float Score { get; init; }

    public required float[] Values { get; init; }

    public required SparseValues SparseValues { get; init; }

    public MetadataValue? Metadata { get; init; }
}

public readonly record struct MetadataValue(object? Inner)
{
    public static implicit operator Google.Protobuf.WellKnownTypes.Value(MetadataValue value)
    {
        throw new NotImplementedException();
    }
}
