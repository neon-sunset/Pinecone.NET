namespace Pinecone.Types;

public readonly record struct PineconeCollectionName(string Name)
{
    public static implicit operator string(PineconeCollectionName value) => value.Name;
}
