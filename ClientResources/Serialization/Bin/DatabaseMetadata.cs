namespace AllodsOnlineEditorTools.ClientResources.Serialization.Bin;

public class DatabaseMetadata
{
    /// <summary>Database format version, read from the header chunk.</summary>
    public required ulong Version { get; init; }

    /// <summary>Version of the resource system that produced the database.</summary>
    public required int ResourceSystemVersion { get; init; }

    /// <summary>
    /// Maps the id stored at offset +12 of each serialized <see cref="DataTypes.TextFileRef"/> to the
    /// referenced txt file path (empty when the ref has none). One entry per TextFileRef occurrence,
    /// with ids assigned globally across all databases of a build; only the main database (pack.bin)
    /// carries the table. The actual text contents live in the localization paks.
    /// </summary>
    public required IDictionary<int, string> TextFileRefNames { get; init; }

    /// <summary>Names of the struct types serialized in the data chunk, in declaration order (the <c>struct NDb::</c> prefix stripped).</summary>
    public required List<string> Structs { get; init; }

    /// <summary>Maps each database id (dbid) to its source file name.</summary>
    public required IDictionary<int, string> Dbid2File { get; init; }

    /// <summary>Reverse of <see cref="Dbid2File"/>: maps a file name to its database id (dbid).</summary>
    public required IDictionary<string, int> File2Dbid { get; init; }

    /// <summary>Maps a resource id (resid) to its database id (dbid).</summary>
    public required IDictionary<int, int> Resid2Dbid { get; init; }

    /// <summary>Reverse of <see cref="Resid2Dbid"/>: maps a database id (dbid) to its resource id (resid).</summary>
    public required IDictionary<int, int> Dbid2Resid { get; init; }

    /// <summary>
    /// Pointer fixups to apply when deserializing, keyed by the byte offset of the pointer within the data chunk.
    /// </summary>
    public required IDictionary<int, PointerFix> Fixes { get; init; }

    /// <summary>
    /// Byte offsets of every serialized pak-indexed file reference
    /// (<see cref="DataTypes.FileRefKind.PakFileRef"/>) in the data chunk, used to bind external file
    /// references to the archives in <see cref="Packs"/>.
    /// </summary>
    public HashSet<int>? PakFileRefOffsets { get; init; }

    /// <summary>Names of the pak archives referenced by the database.</summary>
    public List<string>? Packs { get; init; }

    public string? GetStructType(int dbid)
    {
        if (Fixes.TryGetValue(dbid, out var fix)
            && fix.Type is PointerFix.FixType.Type or PointerFix.FixType.Generic
            && fix.Value >= 0 && fix.Value < Structs.Count)
        {
            return Structs[fix.Value];
        }
        return null;
    }
}