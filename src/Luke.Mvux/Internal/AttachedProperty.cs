using System.Runtime.CompilerServices;

namespace Luke.Mvux.Internal;

internal static class AttachedProperty
{
    private static readonly ConditionalWeakTable<object, Dictionary<object, object>> _table = new();

    public static TValue GetOrCreate<TOwner, TKey, TValue>(
        TOwner owner,
        TKey key,
        Func<TOwner, TKey, TValue> factory)
        where TOwner : class
        where TKey : notnull
        where TValue : class
    {
        var dict = _table.GetOrCreateValue(owner);
        lock (dict)
        {
            if (!dict.TryGetValue(key, out var value))
                dict[key] = value = factory(owner, key)!;
            return (TValue)value;
        }
    }
}
