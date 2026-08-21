#nullable disable
using AllodsOnlineEditorTools.ClientResources.Serialization.Bin;
using JetBrains.Annotations;

namespace AllodsOnlineEditorTools.ClientResources.Structs.Common;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
[StructSize(12)]
public class Blob2
{
    [FieldOffset(4)] public int localId;
    [FieldOffset(8)] public int size;
}
