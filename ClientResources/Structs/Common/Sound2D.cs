#nullable disable

using AllodsOnlineEditorTools.ClientResources.DataTypes;
using AllodsOnlineEditorTools.ClientResources.Serialization.Bin;
using JetBrains.Annotations;

namespace AllodsOnlineEditorTools.ClientResources.Structs.Common;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
[StructSize(24)]
public class Sound2D
{
    [FieldOffset(4)] public string name;
    [FieldOffset(16)] public ResourcePointer project;
}
