using System.Numerics;
using AllodsOnlineEditorTools.ClientResources.DataTypes;
using AllodsOnlineEditorTools.ClientResources.Serialization;
using AllodsOnlineEditorTools.ClientResources.Serialization.Xdb;
using AllodsOnlineEditorTools.ClientResources.Structs.Common;
using AllodsOnlineEditorTools.ClientResources.Structs.V1_1_02_0;
using AllodsOnlineEditorTools.ClientResources.Structs.V1_1_02_0.Enums;

// ReSharper disable UnusedMember.Global
// ReSharper disable InconsistentNaming
#pragma warning disable CS0414 // Field is assigned but its value is never used

namespace ClientResources.Tests;

// Shared fixtures for the xdb and jdb serializer test suites (write + read files).
// Class-level [XdbName] is used by xdb (element / pointer type names) but invisible to jdb, which keys
// $type by the CLR type name — so a single fixture serves both formats.

internal enum SampleEnum
{
    CREATURE_KIND_VERTICAL,
    CREATURE_KIND_HORIZONTAL,
}

internal sealed class IntHolder
{
    public int endFrame = 100000;
}

internal sealed class NegIntHolder
{
    public int fadeDistanceStart = -1;
}

internal sealed class LongHolder
{
    public long crc = 1631568172L;
}

internal sealed class FloatHolder
{
    public float run = 6.5f;
}

internal sealed class DoubleHolder
{
    public double weight = 2.5d;
}

internal sealed class BoolHolder
{
    public bool castShadows = true;
}

internal sealed class StringHolder
{
    public string headBoneName = "Head";
}

internal sealed class NullStringHolder
{
    public string name = null!;
}

internal sealed class EnumRefHolder
{
    [EnumRef(typeof(SampleEnum))] public int kind = 1;
    [EnumRef(typeof(SampleEnum))] public int unknownKind = 42;
    [EnumRef(typeof(SampleEnum))] public int[] kinds = [0, 1, 42];
}

internal sealed class IntArrayHolder
{
    public int[] shaderIndices = [0, 1, 2];
}

internal sealed class EmptyArrayHolder
{
    public int[] objects = [];
}

internal sealed class NullArrayHolder
{
    public int[] objects = null!;
}

internal sealed class FileRefHolder
{
    public FileRef binaryFile = new("Characters/Elf_female/ElfFemale.(Geometry).bin");
}

internal sealed class TextFileRefHolder
{
    public TextFileRef description = new("Texts/description.txt");
}

internal sealed class ResourcePointerHolder
{
    public ResourcePointer surface = new("Material/userinfo.xdb", null);
}

[XdbName("MaterialTemplate")]
internal sealed class SampleMaterial;

[XdbName("CommonMaterialParams")]
internal sealed class SampleParams
{
    public int intensity = 5;
}

internal sealed class NullablePointerHolder
{
    public NullablePointer @params = new(new SampleParams());
}

internal sealed class EmptyNullablePointerHolder
{
    public NullablePointer @params = NullablePointer.Empty;
}

internal sealed class PointerArrayHolder
{
    public NullablePointer[] parts = [new(new SampleParams()), NullablePointer.Empty];
}

internal sealed class Vector2Holder
{
    public Vector2 uv = new(1, 2);
}

internal sealed class Vector3Holder
{
    public Vector3 center = new(1, 2, 3);
}

internal sealed class QuaternionHolder
{
    public Quaternion rotation = new(0, 0, 0, 1);
}

internal sealed class BigVector3Holder
{
    public BigVector3 position = new(0, 0, 0, 1, 2, 3);
}

internal sealed class AabbHolder
{
    public AABB aabb = new() { center = new Vector3(1, 2, 3), extents = new Vector3(4, 5, 6) };
}

internal sealed class RenamedHolder
{
    [XdbName("Name")] public string name = "Foo";
}

[XdbName("gameMechanics.constructor.schemes.item.VisualItem")]
internal sealed class SampleResource
{
    public int dressSlot = 3;
}

internal static class SampleData
{
    public static AnimationProperties AnimationProperties() => new()
    {
        kind = (int)CreatureKind.CREATURE_KIND_SEMIVERTICAL,
        targetTrackingParams = new AnimationProperties.TargetTrackingParams
        {
            verticalRotate = (int)Bone.Spine,
            horizontalRotate = (int)Bone.Head,
            addedToUseAnimations = [(int)Animations.idle, (int)Animations.idle01],
        },
    };
}
