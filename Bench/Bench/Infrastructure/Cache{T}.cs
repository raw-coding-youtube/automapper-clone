namespace Bench.Infrastructure;

public static class Cache<TMapper, TInput, TEntry>
{
    private static Dictionary<Type, TEntry> _cache = new();
    public static bool ContainsKey(Type key) => _cache.ContainsKey(key);
    public static void Add(Type key, TEntry entry) => _cache.Add(key, entry);
    public static TEntry Get(Type key) => _cache[key];
}