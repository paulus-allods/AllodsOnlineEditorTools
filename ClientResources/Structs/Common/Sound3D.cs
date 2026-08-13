#nullable disable

using AllodsOnlineEditorTools.ClientResources.DataTypes;
using AllodsOnlineEditorTools.ClientResources.Serialization.Bin;
using JetBrains.Annotations;


namespace AllodsOnlineEditorTools.ClientResources.Structs.Common;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
[StructSize(28)]
public class Sound3D
{
    [FieldOffset(4)] public float creationRadius;
    [FieldOffset(8)] public string name;
    [FieldOffset(20)] public ResourcePointer project;
}
