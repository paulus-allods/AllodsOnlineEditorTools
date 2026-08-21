using AllodsOnlineEditorTools.ClientResources.Serialization;
using AllodsOnlineEditorTools.ClientResources.Serialization.Jdb;

namespace ClientResources.Tests;

[TestFixture]
public class JdbStructSerializerWriteTests
{
    private static readonly JdbStructSerializer Serializer = new(new JdbStructSerializerOptions(false), ResourceSerializationContext.Default);

    private static string Json(object obj, int resourceId = 0) => Serializer.SerializeResource(obj, resourceId);

    [Test]
    public void Int_SerializesAsNumber() => Assert.That(Json(new IntHolder()),
        Is.EqualTo("""{"$type":"IntHolder","$version":"ClientResources.Tests","endFrame":100000}"""));

    [Test]
    public void NegativeInt_SerializesWithSign() => Assert.That(Json(new NegIntHolder()),
        Is.EqualTo("""{"$type":"NegIntHolder","$version":"ClientResources.Tests","fadeDistanceStart":-1}"""));

    [Test]
    public void Long_SerializesAsNumber() => Assert.That(Json(new LongHolder()),
        Is.EqualTo("""{"$type":"LongHolder","$version":"ClientResources.Tests","crc":1631568172}"""));

    [Test]
    public void Float_SerializesAsNumber() => Assert.That(Json(new FloatHolder()),
        Is.EqualTo("""{"$type":"FloatHolder","$version":"ClientResources.Tests","run":6.5}"""));

    [Test]
    public void Double_SerializesAsNumber() => Assert.That(Json(new DoubleHolder()),
        Is.EqualTo("""{"$type":"DoubleHolder","$version":"ClientResources.Tests","weight":2.5}"""));

    [Test]
    public void Bool_SerializesAsLiteral() => Assert.That(Json(new BoolHolder()),
        Is.EqualTo("""{"$type":"BoolHolder","$version":"ClientResources.Tests","castShadows":true}"""));

    [Test]
    public void String_SerializesAsText() => Assert.That(Json(new StringHolder()),
        Is.EqualTo("""{"$type":"StringHolder","$version":"ClientResources.Tests","headBoneName":"Head"}"""));

    [Test]
    public void NullString_SerializesAsNull() => Assert.That(Json(new NullStringHolder()),
        Is.EqualTo("""{"$type":"NullStringHolder","$version":"ClientResources.Tests","name":null}"""));

    [Test]
    public void EnumRef_MaterializesNamesAndKeepsUnknownNumbers() => Assert.That(Json(new EnumRefHolder()),
        Is.EqualTo(
            """{"$type":"EnumRefHolder","$version":"ClientResources.Tests","kind":"CREATURE_KIND_HORIZONTAL","unknownKind":42,"kinds":["CREATURE_KIND_VERTICAL","CREATURE_KIND_HORIZONTAL",42]}"""));

    [Test]
    public void PrimitiveArray_SerializesAsJsonArray() => Assert.That(Json(new IntArrayHolder()),
        Is.EqualTo("""{"$type":"IntArrayHolder","$version":"ClientResources.Tests","shaderIndices":[0,1,2]}"""));

    [Test]
    public void EmptyArray_SerializesAsEmptyJsonArray() => Assert.That(Json(new EmptyArrayHolder()),
        Is.EqualTo("""{"$type":"EmptyArrayHolder","$version":"ClientResources.Tests","objects":[]}"""));

    [Test]
    public void NullArray_SerializesAsNull() => Assert.That(Json(new NullArrayHolder()),
        Is.EqualTo("""{"$type":"NullArrayHolder","$version":"ClientResources.Tests","objects":null}"""));

    [Test]
    public void FileRef_SerializesAsHrefObject() => Assert.That(Json(new FileRefHolder()),
        Is.EqualTo("""{"$type":"FileRefHolder","$version":"ClientResources.Tests","binaryFile":{"$href":"Characters/Elf_female/ElfFemale.(Geometry).bin"}}"""));

    [Test]
    public void TextFileRef_SerializesAsHrefObject() => Assert.That(Json(new TextFileRefHolder()),
        Is.EqualTo("""{"$type":"TextFileRefHolder","$version":"ClientResources.Tests","description":{"$href":"Texts/description.txt"}}"""));

    [Test]
    public void ResourcePointer_SerializesHrefWithJdbExtension() => Assert.That(Json(new ResourcePointerHolder()),
        Is.EqualTo("""{"$type":"ResourcePointerHolder","$version":"ClientResources.Tests","surface":{"$href":"Material/userinfo.jdb"}}"""));

    [Test]
    public void NullablePointer_SerializesPolymorphicTypeAndFields() => Assert.That(Json(new NullablePointerHolder()),
        Is.EqualTo(
            """{"$type":"NullablePointerHolder","$version":"ClientResources.Tests","params":{"$type":"SampleParams","$version":"ClientResources.Tests","intensity":5}}"""));

    [Test]
    public void EmptyNullablePointer_SerializesAsNull() => Assert.That(Json(new EmptyNullablePointerHolder()),
        Is.EqualTo("""{"$type":"EmptyNullablePointerHolder","$version":"ClientResources.Tests","params":null}"""));

    [Test]
    public void NullablePointerArray_WalksEachElement() => Assert.That(Json(new PointerArrayHolder()),
        Is.EqualTo(
            """{"$type":"PointerArrayHolder","$version":"ClientResources.Tests","parts":[{"$type":"SampleParams","$version":"ClientResources.Tests","intensity":5},null]}"""));

    [Test]
    public void Vector2_SerializesXy() => Assert.That(Json(new Vector2Holder()),
        Is.EqualTo("""{"$type":"Vector2Holder","$version":"ClientResources.Tests","uv":{"x":1,"y":2}}"""));

    [Test]
    public void Vector3_SerializesXyz() => Assert.That(Json(new Vector3Holder()),
        Is.EqualTo("""{"$type":"Vector3Holder","$version":"ClientResources.Tests","center":{"x":1,"y":2,"z":3}}"""));

    [Test]
    public void Quaternion_SerializesXyzw() => Assert.That(Json(new QuaternionHolder()),
        Is.EqualTo("""{"$type":"QuaternionHolder","$version":"ClientResources.Tests","rotation":{"x":0,"y":0,"z":0,"w":1}}"""));

    [Test]
    public void BigVector3_SerializesXyz() => Assert.That(Json(new BigVector3Holder()),
        Is.EqualTo("""{"$type":"BigVector3Holder","$version":"ClientResources.Tests","position":{"x":1,"y":2,"z":3}}"""));

    [Test]
    public void NestedStruct_ReflectsFields() => Assert.That(Json(new AabbHolder()),
        Is.EqualTo("""{"$type":"AabbHolder","$version":"ClientResources.Tests","aabb":{"center":{"x":1,"y":2,"z":3},"extents":{"x":4,"y":5,"z":6}}}"""));

    [Test]
    public void NestedEnumRef_MaterializesNamesAtDepth()
    {
        var json = Json(SampleData.AnimationProperties());
        using (Assert.EnterMultipleScope())
        {
            Assert.That(json, Does.Contain("\"kind\":\"CREATURE_KIND_SEMIVERTICAL\""));
            Assert.That(json, Does.Contain("\"verticalRotate\":\"Spine\""));
            Assert.That(json, Does.Contain("\"horizontalRotate\":\"Head\""));
            Assert.That(json, Does.Contain("\"addedToUseAnimations\":[\"idle\",\"idle01\"]"));
        }
    }

    [Test]
    public void XdbName_KeysFieldByOverride() => Assert.That(Json(new RenamedHolder()),
        Is.EqualTo("""{"$type":"RenamedHolder","$version":"ClientResources.Tests","Name":"Foo"}"""));

    [Test]
    public void Resource_WithId_EmitsResourceId() => Assert.That(Json(new SampleResource(), 564002825),
        Is.EqualTo("""{"$resourceId":564002825,"$type":"SampleResource","$version":"ClientResources.Tests","dressSlot":3}"""));

    [Test]
    public void Resource_WithoutId_OmitsResourceId() => Assert.That(Json(new SampleResource()),
        Is.EqualTo("""{"$type":"SampleResource","$version":"ClientResources.Tests","dressSlot":3}"""));
}
