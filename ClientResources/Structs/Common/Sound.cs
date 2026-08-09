#nullable disable

using AllodsOnlineEditorTools.ClientResources.DataTypes;
using AllodsOnlineEditorTools.ClientResources.Serialization.Bin;
using JetBrains.Annotations;

namespace AllodsOnlineEditorTools.ClientResources.Structs.Common;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
[StructSize(24)]
public class Sound
{
    [FieldOffset(4)] public ResourcePointer project;
    [FieldOffset(12)] public string name;
}
