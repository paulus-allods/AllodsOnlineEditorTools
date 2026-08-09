using System.Collections.Concurrent;

namespace AllodsOnlineEditorTools.ClientResources.Serialization;

/// <summary>
/// Process-wide cache of <see cref="StructTypeResolver"/>s keyed by version namespace. A jdb read carries its
/// source version in <c>$version</c>, so a single run may read documents from several versions; this scans
/// each namespace only once. Resolvers are immutable, so the shared instances are safe across threads.
/// </summary>
public static class StructTypeResolverCache
{
    private static readonly ConcurrentDictionary<string, StructTypeResolver> Cache = new();

    public static StructTypeResolver ForNamespace(string versionNamespace) =>
        Cache.GetOrAdd(versionNamespace, static ns => StructTypeResolver.FromNamespace(ns));
}
