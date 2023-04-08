using Google.Protobuf.Collections;
using Microsoft.Extensions.ObjectPool;

namespace Pinecone.Transport.Grpc;

internal sealed class RepeatedFieldPolicy : PooledObjectPolicy<RepeatedField<float>>
{
    public override RepeatedField<float> Create()
    {
        return new() { Capacity = 8 };
    }

    public override bool Return(RepeatedField<float> obj)
    {
        for (var i = 0; i < obj.Count; i++)
        {
            obj[i] = default;
        }

        return true;
    }
}
