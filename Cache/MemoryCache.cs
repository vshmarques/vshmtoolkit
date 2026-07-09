using System.Collections.Concurrent;

namespace VshmToolkit.Cache;

public sealed class MemoryCache<T>
{
    private static readonly Lazy<MemoryCache<T>> _instance = new Lazy<MemoryCache<T>>(() => new MemoryCache<T>());

    private readonly ConcurrentDictionary<string, T> _data;

    private MemoryCache()
    {
        _data = new ConcurrentDictionary<string, T>();
    }

    public static MemoryCache<T> Instance => _instance.Value;

    public void Set(string key, T value)
    {
        _data[key] = value;
    }

    public T? Get(string key)
    {
        return _data.TryGetValue(key, out var value) ? value : default;
    }

    public bool Remove(string key)
    {
        return _data.TryRemove(key, out _);
    }

    public void Clear()
    {
        _data.Clear();
    }
}