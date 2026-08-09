namespace AllodsOnlineEditorTools.ClientResources.Serialization.Bin;

public readonly record struct PointerFix(PointerFix.FixType Type, bool External, int Value)
{
    public enum FixType
    {
        DbIdRef,
        Direct,
        Type,
        Generic,
    }
}
