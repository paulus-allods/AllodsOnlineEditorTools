using System.Numerics;
using System.Xml.Linq;
using AllodsOnlineEditorTools.ClientResources.DataTypes;
using AllodsOnlineEditorTools.ClientResources.Serialization;
using AllodsOnlineEditorTools.ClientResources.Serialization.Xdb;
using AllodsOnlineEditorTools.ClientResources.Structs.Common;
using AllodsOnlineEditorTools.ClientResources.Structs.V1_1_02_0;
using AllodsOnlineEditorTools.ClientResources.Structs.V1_1_02_0.Enums;

namespace ClientResources.Tests;

/// <summary>
/// Read side of <see cref="XdbStructSerializer"/>. xdb carries no type metadata, so the reader is given a
/// <see cref="StructTypeResolver"/> (built here from the fixtures + real structs it must resolve). Coverage is
/// via round-trips: write with the writer, parse back with the reader, assert. Floats use XdbFloat-safe
/// values since xdb float writes are lossy (6 significant digits). Write side:
/// <see cref="XdbStructSerializerWriteTests"/>.
/// </summary>
[TestFixture]
public class XdbStructSerializerReadTests
{
    private static readonly XdbStructSerializer Writer = new(XdbStructSerializerOptions.Default, ResourceSerializationContext.Default);

    private static readonly XdbStructSerializer Reader = new(
        XdbStructSerializerOptions.Default,
        new ResourceSerializationContext
        {
            TypeResolver = StructTypeResolver.FromTypes(
                typeof(IntHolder), typeof(LongHolder), typeof(FloatHolder), typeof(DoubleHolder),
                typeof(BoolHolder), typeof(StringHolder), typeof(EnumRefHolder), typeof(IntArrayHolder),
                typeof(FileRefHolder), typeof(TextFileRefHolder), typeof(NullablePointerHolder),
                typeof(EmptyNullablePointerHolder), typeof(Vector2Holder), typeof(Vector3Holder),
                typeof(QuaternionHolder), typeof(BigVector3Holder), typeof(AabbHolder), typeof(RenamedHolder),
                typeof(SampleResource), typeof(SampleParams),
                typeof(AABB), typeof(PredicateHonorRankLess), typeof(AnimationProperties), typeof(AstralIslandTeleport)),
        });

    private static T RoundTrip<T>(T obj) where T : notnull
        => (T)Reader.ParseResource(Writer.SerializeResource(obj, 0), out _);

    private static T RoundTripField<T>(T value)
        => (T)Reader.DeserializeField(Writer.SerializeField(value, "field", typeof(T))!, typeof(T))!;
    
    [Test] public void Int() => Assert.That(RoundTripField(100000), Is.EqualTo(100000));
    [Test] public void NegativeInt() => Assert.That(RoundTripField(-1), Is.EqualTo(-1));
    [Test] public void Long() => Assert.That(RoundTripField(1631568172L), Is.EqualTo(1631568172L));
    [Test] public void Bool() => Assert.That(RoundTripField(true), Is.True);
    [Test] public void Float() => Assert.That(RoundTripField(6.5f), Is.EqualTo(6.5f)); // XdbFloat-safe
    [Test] public void Double() => Assert.That(RoundTripField(2.5d), Is.EqualTo(2.5d));
    [Test] public void String() => Assert.That(RoundTripField("Head"), Is.EqualTo("Head"));
    [Test] public void EmptyString() => Assert.That(RoundTripField(""), Is.EqualTo(""));
    [Test] public void PrimitiveArray() => Assert.That(RoundTripField(new[] { 0, 1, 2 }), Is.EqualTo([0, 1, 2]));
    [Test] public void StringArray() => Assert.That(RoundTripField(new[] { "vs_2_0", "ps_2_0" }), Is.EqualTo(["vs_2_0", "ps_2_0"]));
    [Test] public void FileRef() => Assert.That(RoundTripField(new FileRef("a/b.bin")).Name, Is.EqualTo("a/b.bin"));
    [Test] public void TextFileRef() => Assert.That(RoundTripField(new TextFileRef("a/b.txt")).Name, Is.EqualTo("a/b.txt"));
    [Test] public void Vector2() => Assert.That(RoundTripField(new Vector2(1, 2)), Is.EqualTo(new Vector2(1, 2)));
    [Test] public void Vector3() => Assert.That(RoundTripField(new Vector3(1, 2, 3)), Is.EqualTo(new Vector3(1, 2, 3)));
    [Test] public void Quaternion() => Assert.That(RoundTripField(new Quaternion(0, 0, 0, 1)), Is.EqualTo(new Quaternion(0, 0, 0, 1)));
    [Test] public void BigVector3() => Assert.That(RoundTripField(new BigVector3(0, 0, 0, 1.25f, 2.5f, 3.75f)), Is.EqualTo(new BigVector3(0, 0, 0, 1.25f, 2.5f, 3.75f)));
    
    [Test]
    public void ResourcePointer_StripsRootAndXpointer()
        => Assert.That(RoundTripField(new ResourcePointer("Material/userinfo.xdb", typeof(SampleMaterial))).Href,
            Is.EqualTo("Material/userinfo.xdb"));

    [Test]
    public void EnumRef_RoundTripsCarrierInts()
    {
        var parsed = RoundTrip(new EnumRefHolder());
        using (Assert.EnterMultipleScope())
        {
            Assert.That(parsed.kind, Is.EqualTo(1));
            Assert.That(parsed.unknownKind, Is.EqualTo(42));
            Assert.That(parsed.kinds, Is.EqualTo([0, 1, 42]));
        }
    }

    [Test]
    public void Array_RoundTrips()
        => Assert.That(RoundTrip(new IntArrayHolder()).shaderIndices, Is.EqualTo([0, 1, 2]));

    [Test]
    public void FileRefs_RoundTrip()
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(RoundTrip(new FileRefHolder()).binaryFile.Name, Is.EqualTo("Characters/Elf_female/ElfFemale.(Geometry).bin"));
            Assert.That(RoundTrip(new TextFileRefHolder()).description.Name, Is.EqualTo("Texts/description.txt"));
        }
    }

    [Test]
    public void Vectors_RoundTrip()
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(RoundTrip(new Vector2Holder()).uv, Is.EqualTo(new Vector2(1, 2)));
            Assert.That(RoundTrip(new Vector3Holder()).center, Is.EqualTo(new Vector3(1, 2, 3)));
            Assert.That(RoundTrip(new QuaternionHolder()).rotation, Is.EqualTo(new Quaternion(0, 0, 0, 1)));
            Assert.That(RoundTrip(new BigVector3Holder()).position, Is.EqualTo(new BigVector3(0, 0, 0, 1, 2, 3)));
        }
    }

    [Test]
    public void NestedStruct_RoundTrips()
    {
        var parsed = RoundTrip(new AabbHolder());
        using (Assert.EnterMultipleScope())
        {
            Assert.That(parsed.aabb.center, Is.EqualTo(new Vector3(1, 2, 3)));
            Assert.That(parsed.aabb.extents, Is.EqualTo(new Vector3(4, 5, 6)));
        }
    }

    [Test]
    public void NullablePointer_RoundTripsPolymorphicTarget()
        => Assert.That(((SampleParams)RoundTrip(new NullablePointerHolder()).@params.Value!).intensity, Is.EqualTo(5));
    
    [Test]
    public void EmptyNullablePointer_ReadsBackAsEmpty()
        => Assert.That(RoundTrip(new EmptyNullablePointerHolder()).@params.Value, Is.Null);

    [Test]
    public void RenamedField_RoundTrips()
        => Assert.That(RoundTrip(new RenamedHolder()).name, Is.EqualTo("Foo"));
    
    [Test]
    public void ParseResource_ReadsRootTypeAndResourceId()
    {
        var xml = Writer.SerializeResource(new SampleResource(), 564002825);
        var parsed = (SampleResource)Reader.ParseResource(xml, out var resourceId);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(resourceId, Is.EqualTo(564002825));
            Assert.That(parsed.dressSlot, Is.EqualTo(3));
        }
    }

    [Test]
    public void RoundTrip_RealNestedVectors()
    {
        // XdbFloat-safe components (exact under 6-significant-digit rounding).
        var original = new AABB { center = new Vector3(1.5f, -2f, 3f), extents = new Vector3(4f, 5f, 6.25f) };
        var parsed = RoundTrip(original);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(parsed.center, Is.EqualTo(original.center));
            Assert.That(parsed.extents, Is.EqualTo(original.extents));
        }
    }

    [Test]
    public void RoundTrip_RealEnumRef_KnownAndUnknown()
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(RoundTrip(new PredicateHonorRankLess { rank = (int)HonorRank.HRButcher }).rank, Is.EqualTo((int)HonorRank.HRButcher));
            Assert.That(RoundTrip(new PredicateHonorRankLess { rank = 99 }).rank, Is.EqualTo(99));
        }
    }

    [Test]
    public void RoundTrip_RealNestedEnumRef()
    {
        var parsed = RoundTrip(SampleData.AnimationProperties());
        using (Assert.EnterMultipleScope())
        {
            Assert.That(parsed.kind, Is.EqualTo((int)CreatureKind.CREATURE_KIND_SEMIVERTICAL));
            Assert.That(parsed.targetTrackingParams.verticalRotate, Is.EqualTo((int)Bone.Spine));
            Assert.That(parsed.targetTrackingParams.horizontalRotate, Is.EqualTo((int)Bone.Head));
            Assert.That(parsed.targetTrackingParams.addedToUseAnimations,
                Is.EqualTo([(int)Animations.idle, (int)Animations.idle01]));
        }
    }
    
    [Test]
    public void RoundTrip_RealNullablePointerArray()
    {
        var original = new AstralIslandTeleport
        {
            parts =
            [
                new NullablePointer(new PredicateHonorRankLess { rank = (int)HonorRank.HRKiller }),
                new NullablePointer(new PredicateHonorRankLess { rank = (int)HonorRank.HRJudge }),
            ],
        };

        var parsed = RoundTrip(original);

        Assert.That(parsed.parts, Has.Length.EqualTo(2));
        using (Assert.EnterMultipleScope())
        {
            Assert.That(((PredicateHonorRankLess)parsed.parts[0].Value!).rank, Is.EqualTo((int)HonorRank.HRKiller));
            Assert.That(((PredicateHonorRankLess)parsed.parts[1].Value!).rank, Is.EqualTo((int)HonorRank.HRJudge));
        }
    }
}
