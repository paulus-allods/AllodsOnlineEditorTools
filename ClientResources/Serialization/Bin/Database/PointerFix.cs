namespace AllodsOnlineEditorTools.ClientResources.Serialization.Bin.Database;

public readonly record struct PointerFix(PointerFix.FixType Type, bool External, long Value)
{
    public enum FixType
    {
        DbIdRef,
        Direct,
        Type,
        Generic,
    }
}
