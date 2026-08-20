namespace AllodsOnlineEditorTools.ClientResources.Serialization.Bin.Database;

/// <summary>
/// Identifies a chunk in a <c>.bin</c> database payload.
/// </summary>
internal enum DatabaseChunkId
{
    Header = 0,
    TxtFiles = 1,
    Metadata = 2,
    Data = 3,
    Fixes = 4,
    PakFileRefs = 5,
    Packs = 6,
}
