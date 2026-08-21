using System.Collections.Concurrent;

namespace AllodsOnlineEditorTools.ClientResources.Serialization;

/// <summary>
/// Resolves the first registered converter that accepts a type, caching the result per type.
/// Order matters: the first converter whose <see cref="ITypeConverter.CanConvert"/> matches wins.
/// A registry holds no serialization context, so instances are safe to share across databases and threads.
/// </summary>
public abstract class ConverterRegistry<TConverter>(IReadOnlyList<TConverter> converters) where TConverter : class, ITypeConverter
{
    private readonly ConcurrentDictionary<Type, TConverter?> _cache = new();

    public TConverter? GetConverter(Type type)
    {
        return _cache.GetOrAdd(type, static (t, cs) => cs.FirstOrDefault(c => c.CanConvert(t)), converters);
    }
}
