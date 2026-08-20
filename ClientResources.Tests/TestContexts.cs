using AllodsOnlineEditorTools.ClientResources.DataTypes;
using AllodsOnlineEditorTools.ClientResources.Serialization;
using AllodsOnlineEditorTools.ClientResources.Serialization.Bin;
using AllodsOnlineEditorTools.ClientResources.Serialization.Bin.Database;

namespace ClientResources.Tests;

internal static class TestContexts
{
    private static DatabaseMetadata EmptyMetadata() => new()
    {
        Version = [],
        ResourceSystemVersion = 0,
        TextFileRefNames = new Dictionary<int, string>(),
        Structs = [],
        DbId2File = new Dictionary<long, string>(),
        File2DbId = new Dictionary<string, long>(),
        ResId2DbId = new Dictionary<int, long>(),
        DbId2ResId = new Dictionary<long, int>(),
        Fixes = new Dictionary<long, PointerFix>(),
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
