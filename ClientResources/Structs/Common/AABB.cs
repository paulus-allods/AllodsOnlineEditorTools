#nullable disable

using System.Numerics;
using AllodsOnlineEditorTools.ClientResources.Serialization.Bin;
using JetBrains.Annotations;

namespace AllodsOnlineEditorTools.ClientResources.Structs.Common;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
[StructSize(24)]
public class AABB
{
    [FieldOffset(0)] public Vector3 center;
    [FieldOffset(12)] public Vector3 extents;
}
