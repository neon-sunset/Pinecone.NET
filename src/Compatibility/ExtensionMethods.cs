namespace System.Linq
{
#if NETSTANDARD2_0
    public static class ExtensionMethods
    {
        public static void Deconstruct<T1, T2>(this KeyValuePair<T1, T2> tuple, out T1 key, out T2 value)
        {
            key = tuple.Key;
            value = tuple.Value;
        }
    }
#endif
}
