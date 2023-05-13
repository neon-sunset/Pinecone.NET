namespace Pinecone;

public readonly record struct CollectionName(string Value)
{
    public static implicit operator string(CollectionName value) => value.Value;
    public static implicit operator CollectionName(string value) => new(value);
}

public record CollectionDetails
{
    public required string Name { get; init; }
    public required uint Size { get; init; }
    public required string Status { get; init; }
}
