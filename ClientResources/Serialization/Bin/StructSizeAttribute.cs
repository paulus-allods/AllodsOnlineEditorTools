namespace AllodsOnlineEditorTools.ClientResources.Serialization.Bin;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public class StructSizeAttribute(int size) : Attribute
{
    public int Size { get; } = size;
}
