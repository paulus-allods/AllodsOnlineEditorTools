using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using AllodsOnlineEditorTools.ClientResources.Serialization.Xdb;
using AllodsOnlineEditorTools.ClientResources.Structs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AllodsOnlineEditorTools.ClientResources.Serialization;

/// <summary>
/// Resolves a struct's document identity back to its CLR type for one game version. Construction runs a
/// single reflection scan of the version's namespace into two indexes so each format keeps its own key rule:
/// by CLR <see cref="Type.Name"/> (Bin metadata + Jdb <c>$type</c>, non-nested — the flat name the
/// binary/JSON carry) and by <see cref="XdbNameAttribute"/> (xdb element name, nested types included).
/// Instances are immutable and built through the factory methods, so they are safe to share across databases
/// and threads; <see cref="StructTypeResolverCache"/> memoizes one per namespace.
/// </summary>
public sealed class StructTypeResolver
{
    private readonly IReadOnlyDictionary<string, Type> _byXdbName;

    private StructTypeResolver(IReadOnlyDictionary<string, Type> byName, IReadOnlyDictionary<string, Type> byXdbName)
    {
        ByName = byName;
        _byXdbName = byXdbName;
    }

    public IReadOnlyDictionary<string, Type> ByName { get; }

    public IEnumerable<Type> Types => ByName.Values;

    public bool TryResolveByName(string name, [NotNullWhen(true)] out Type? type) => ByName.TryGetValue(name, out type);

    public Type ResolveByName(string name) =>
        ByName.TryGetValue(name, out var type)
            ? type
            : throw new InvalidOperationException($"No struct type is registered for name '{name}'");

    public Type ResolveByXdbName(string xdbName) =>
        _byXdbName.TryGetValue(xdbName, out var type)
            ? type
            : throw new InvalidOperationException($"No struct type is registered for xdb name '{xdbName}'");

    public static StructTypeResolver FromVersion(GameVersion version, ILogger<StructTypeResolver>? logger = null) =>
        FromNamespace(version.FullNamespace, logger);

    public static StructTypeResolver FromNamespace(string versionNamespace, ILogger<StructTypeResolver>? logger = null)
    {
        var log = (ILogger?)logger ?? NullLogger.Instance;
        log.LogInformation("Start loading structs for namespace {Namespace}", versionNamespace);

        var byName = new Dictionary<string, Type>();
        var byXdbName = new Dictionary<string, Type>();
        foreach (var type in Assembly.GetExecutingAssembly().GetTypes())
        {
            if (type is not { IsClass: true, IsNested: false, Namespace: not null }
                || !IsInNamespace(type.Namespace, versionNamespace))
            {
                continue;
            }

            byName[type.Name] = type;
            var xdbName = XdbNameAttribute.Resolve(type);
            if (byXdbName.TryGetValue(xdbName, out var existing) && existing != type)
            {
                log.LogWarning("Duplicate xdb name '{XdbName}' for {Existing} and {Type}; last wins", xdbName,
                    existing.FullName, type.FullName);
            }

            byXdbName[xdbName] = type;
        }

        return new StructTypeResolver(byName, byXdbName);
    }

    public static StructTypeResolver FromTypes(params Type[] types) =>
        new(types.Where(t => !t.IsNested).ToDictionary(t => t.Name),
            types.ToDictionary(XdbNameAttribute.Resolve));

    private static bool IsInNamespace(string typeNamespace, string versionNamespace)
        => typeNamespace == versionNamespace ||
           typeNamespace.StartsWith(versionNamespace + ".", StringComparison.Ordinal);
}
