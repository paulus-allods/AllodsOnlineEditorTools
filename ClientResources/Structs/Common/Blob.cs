#nullable disable
using AllodsOnlineEditorTools.ClientResources.Serialization.Bin;
using JetBrains.Annotations;

namespace AllodsOnlineEditorTools.ClientResources.Structs.Common;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
[StructSize(12)]
public class Blob
{
    [FieldOffset(4)] public int size;
    [FieldOffset(8)] public int localId;
}
