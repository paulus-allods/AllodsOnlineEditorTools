using AllodsOnlineEditorTools.ClientResources.DataTypes;
using AllodsOnlineEditorTools.ClientResources.Serialization;
using AllodsOnlineEditorTools.ClientResources.Serialization.Bin;

namespace ClientResources.Tests;

internal static class TestContexts
{
    private static DatabaseMetadata EmptyMetadata() => new()
    {
        Version = 0,
        ResourceSystemVersion = 0,
        TextFileRefNames = new Dictionary<int, string>(),
        Structs = [],
        Dbid2File = new Dictionary<int, string>(),
        File2Dbid = new Dictionary<string, int>(),
        Resid2Dbid = new Dictionary<int, int>(),
        Dbid2Resid = new Dictionary<int, int>(),
        Fixes = new Dictionary<int, PointerFix>(),
        Packs = null,
    };

    public static BinaryStructSerializerContext TestContext(FileRefKind kind)
    {
        var metadata = EmptyMetadata();
        return new BinaryStructSerializerContext
        {
            CurrentDatabaseMetadata = metadata,
            MainDatabaseMetadata = metadata,
            TypeResolver = StructTypeResolver.FromTypes(),
            FileRefKind = kind,
            Packs = null,
        };
    }
}