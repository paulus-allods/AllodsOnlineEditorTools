namespace AllodsOnlineEditorTools.ClientResources.Serialization.Bin.Database;

/// <summary>
/// The pointer width of a memory-image database and every layout constant derived from it.
/// V2 packs ship in 32-bit and 64-bit builds; all their width differences reduce to
/// <see cref="PointerSize"/>, so this is the single knob that distinguishes the two.
/// </summary>
internal sealed class WordSize(int pointerSize)
{
    public static readonly WordSize X86 = new(4);
    public static readonly WordSize X64 = new(8);

    /// <summary>Size in bytes of a pointer / pointer-sized value (4 or 8).</summary>
    public int PointerSize => pointerSize;

    /// <summary>Size in bytes of one Fixes entry (a pointer-sized <c>data</c> plus a pointer-sized <c>value</c>).</summary>
    public int FixEntrySize => 2 * PointerSize;

    /// <summary>Multiplier turning a fix's packed address (in pointer units) into a byte offset into the data chunk.</summary>
    public int FixAddressScale => PointerSize;

    /// <summary>Constant subtracted when decoding a pak-file-ref offset (three pointer widths).</summary>
    public int PakFileRefSubtrahend => 3 * PointerSize;

    /// <summary>Reads a little-endian pointer-sized value (4- or 8-byte) from the reader.</summary>
    public long ReadWord(BinaryReader reader) => PointerSize == 8 ? reader.ReadInt64() : reader.ReadInt32();
}
