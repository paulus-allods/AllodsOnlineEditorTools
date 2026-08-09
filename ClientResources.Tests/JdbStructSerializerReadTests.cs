using System.Numerics;
using System.Text.Json;
using AllodsOnlineEditorTools.ClientResources.DataTypes;
using AllodsOnlineEditorTools.ClientResources.Serialization;
using AllodsOnlineEditorTools.ClientResources.Serialization.Jdb;
using AllodsOnlineEditorTools.ClientResources.Structs.Common;
using AllodsOnlineEditorTools.ClientResources.Structs.V1_1_02_0;
using AllodsOnlineEditorTools.ClientResources.Structs.V1_1_02_0.Enums;

namespace ClientResources.Tests;

[TestFixture]
public class JdbStructSerializerReadTests
{
    private static readonly JdbStructSerializer Serializer =
        new(new JdbStructSerializerOptions(false), ResourceSerializationContext.Default);
    
    [Test]
    public void RoundTrip_NestedVectors()
    {
        var original = new AABB { center = new Vector3(1.5f, -2f, 3f), extents = new Vector3(4f, 5f, 6.25f) };
        var parsed = (AABB)Serializer.ParseResource(Serializer.SerializeResource(original, 0), out _);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(parsed.center, Is.EqualTo(original.center));
            Assert.That(parsed.extents, Is.EqualTo(original.extents));
        }
    }

    [Test]
    public void RoundTrip_EnumRef_KnownValue()
    {
        var original = new PredicateHonorRankLess { rank = (int)HonorRank.HRButcher };
        var parsed = (PredicateHonorRankLess)Serializer.ParseResource(Serializer.SerializeResource(original, 0), out _);
        Assert.That(parsed.rank, Is.EqualTo((int)HonorRank.HRButcher));
    }

    [Test]
    public void RoundTrip_EnumRef_UnknownValueKeepsNumber()
    {
        var original = new PredicateHonorRankLess { rank = 99 };
        var parsed = (PredicateHonorRankLess)Serializer.ParseResource(Serializer.SerializeResource(original, 0), out _);
        Assert.That(parsed.rank, Is.EqualTo(99));
    }

    [Test]
    public void RoundTrip_PreservesResourceId()
    {
        var original = new PredicateHonorRankLess { rank = (int)HonorRank.HRJudge };
        var json = Serializer.SerializeResource(original, 12345);
        var parsed = (PredicateHonorRankLess)Serializer.ParseResource(json, out var resourceId);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(resourceId, Is.EqualTo(12345));
            Assert.That(parsed.rank, Is.EqualTo((int)HonorRank.HRJudge));
        }
    }

    [Test]
    public void RoundTrip_NestedEnumRef()
    {
        var original = SampleData.AnimationProperties();
        var parsed = (AnimationProperties)Serializer.ParseResource(Serializer.SerializeResource(original, 0), out _);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(parsed.kind, Is.EqualTo(original.kind));
            Assert.That(parsed.targetTrackingParams.verticalRotate, Is.EqualTo((int)Bone.Spine));
            Assert.That(parsed.targetTrackingParams.horizontalRotate, Is.EqualTo((int)Bone.Head));
            Assert.That(parsed.targetTrackingParams.addedToUseAnimations,
                Is.EqualTo([(int)Animations.idle, (int)Animations.idle01]));
        }
    }
    
    [Test]
    public void RoundTrip_NullablePointerArray()
    {
        var original = new AstralIslandTeleport
        {
            parts =
            [
                new NullablePointer(new PredicateHonorRankLess { rank = (int)HonorRank.HRKiller }),
                NullablePointer.Empty,
                new NullablePointer(new PredicateHonorRankLess { rank = (int)HonorRank.HRJudge }),
            ],
        };

        var parsed = (AstralIslandTeleport)Serializer.ParseResource(Serializer.SerializeResource(original, 0), out _);

        Assert.That(parsed.parts, Has.Length.EqualTo(3));
        using (Assert.EnterMultipleScope())
        {
            Assert.That(((PredicateHonorRankLess)parsed.parts[0].Value!).rank, Is.EqualTo((int)HonorRank.HRKiller));
            Assert.That(parsed.parts[1].Value, Is.Null);
            Assert.That(((PredicateHonorRankLess)parsed.parts[2].Value!).rank, Is.EqualTo((int)HonorRank.HRJudge));
        }
    }
    
    private static T RoundTripField<T>(T value)
    {
        var tree = Serializer.SerializeField(value, typeof(T), null);
        using var doc = JsonDocument.Parse(JsonSerializer.Serialize(tree));
        return (T)Serializer.DeserializeField(doc.RootElement, typeof(T), null)!;
    }

    [Test] public void RoundTripField_Vector2() => Assert.That(RoundTripField(new Vector2(1.5f, -2f)), Is.EqualTo(new Vector2(1.5f, -2f)));
    [Test] public void RoundTripField_Vector3() => Assert.That(RoundTripField(new Vector3(1f, 2f, 3f)), Is.EqualTo(new Vector3(1f, 2f, 3f)));
    [Test] public void RoundTripField_Quaternion() => Assert.That(RoundTripField(new Quaternion(0.1f, 0.2f, 0.3f, 0.4f)), Is.EqualTo(new Quaternion(0.1f, 0.2f, 0.3f, 0.4f)));
    [Test] public void RoundTripField_BigVector3() => Assert.That(RoundTripField(new BigVector3(0, 0, 0, 1, 2, 3)), Is.EqualTo(new BigVector3(0, 0, 0, 1, 2, 3)));
    [Test] public void RoundTripField_FileRef() => Assert.That(RoundTripField(new FileRef("a/b.bin")).Name, Is.EqualTo("a/b.bin"));
    [Test] public void RoundTripField_TextFileRef() => Assert.That(RoundTripField(new TextFileRef("a/b.txt")).Name, Is.EqualTo("a/b.txt"));
    [Test] public void RoundTripField_WString() => Assert.That(RoundTripField(new WString("héllo")).Value, Is.EqualTo("héllo"));
    [Test] public void RoundTripField_Enum() => Assert.That(RoundTripField(SampleEnum.CREATURE_KIND_HORIZONTAL), Is.EqualTo(SampleEnum.CREATURE_KIND_HORIZONTAL));
    
    [Test]
    public void RoundTripField_ResourcePointer_HrefBecomesJdb()
        => Assert.That(RoundTripField(new ResourcePointer("Material/userinfo.xdb", null)).Href, Is.EqualTo("Material/userinfo.jdb"));
}
