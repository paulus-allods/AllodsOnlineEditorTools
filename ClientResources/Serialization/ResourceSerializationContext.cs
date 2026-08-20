using AllodsOnlineEditorTools.ClientResources.Structs;

namespace AllodsOnlineEditorTools.ClientResources.Serialization;

public sealed class ResourceSerializationContext
{
    public static ResourceSerializationContext Default { get; } = new();

    public IReadOnlyDictionary<(Type DeclaringType, string FieldName), Type?>? EnumRefOverrides { get; init; }
    public StructTypeResolver? TypeResolver { get; init; }

    public Type? ResolveEnumRef(StructField field)
    {
        if (field.DeclaringType is not null
            && EnumRefOverrides is not null
            && EnumRefOverrides.TryGetValue((field.DeclaringType, field.Name), out var overrideEnum))
        {
            return overrideEnum;
        }

        return field.EnumRef;
    }

    public Type ResolveByXdbName(string xdbName) =>
        (TypeResolver ?? throw new InvalidOperationException(
            "xdb read needs a StructTypeResolver (the version must be supplied explicitly); none was provided"))
        .ResolveByXdbName(xdbName);

    public Type ResolveDocumentType(string versionNamespace, string typeName)
    {
        if (!versionNamespace.StartsWith(GameVersion.StructsNamespace, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"$version '{versionNamespace}' is not a known structs namespace");
        }

        return StructTypeResolverCache.ForNamespace(versionNamespace).ResolveByName(typeName);
    }
}
