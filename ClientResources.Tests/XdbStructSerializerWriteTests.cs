using System.Numerics;
using System.Xml.Linq;
using AllodsOnlineEditorTools.ClientResources.DataTypes;
using AllodsOnlineEditorTools.ClientResources.Serialization;
using AllodsOnlineEditorTools.ClientResources.Serialization.Xdb;
using AllodsOnlineEditorTools.ClientResources.Structs.Common;

namespace ClientResources.Tests;

/// <summary>
/// Write side of <see cref="XdbStructSerializer"/>: it produces the xdb layout used by real 1.1 game
/// resources, one test per field type. Expected fragments come from real sample xdb files (see comments),
/// compared without formatting so only element/attribute structure matters. Fixtures live in
/// <c>SerializerTestFixtures.cs</c>; the read side is in <see cref="XdbStructSerializerReadTests"/>.
/// </summary>
[TestFixture]
public class XdbStructSerializerWriteTests
{
    private static readonly XdbStructSerializer Serializer = new(XdbStructSerializerOptions.Default, ResourceSerializationContext.Default);

    private static XElement? Field(object? value, string name, Type type) => Serializer.SerializeField(value, name, type);

    private static string Xml(XElement? element) => element!.ToString(SaveOptions.DisableFormatting);

    // ---------------------------------------------------------------- Primitives

    // Sample: <endFrame>100000</endFrame> (Geometry.xdb)
    [Test]
    public void Int_SerializesAsText() =>
        Assert.That(Xml(Field(100000, "endFrame", typeof(int))), Is.EqualTo("<endFrame>100000</endFrame>"));

    // Sample: <fadeDistanceStart>-1</fadeDistanceStart> (Geometry.xdb)
    [Test]
    public void NegativeInt_SerializesWithSign() => Assert.That(Xml(Field(-1, "fadeDistanceStart", typeof(int))),
        Is.EqualTo("<fadeDistanceStart>-1</fadeDistanceStart>"));

    // Sample: <fmodProjectCRC>-1945242361</fmodProjectCRC> (Weapon.(FMODProject).xdb)
    [Test]
    public void Long_SerializesAsText() => Assert.That(Xml(Field(1631568172L, "crc", typeof(long))), Is.EqualTo("<crc>1631568172</crc>"));

    // Sample: <run>6.5</run> (AnimationProperties.xdb). Float goes through XdbFloat.ToXdbString.
    [Test]
    public void Float_UsesXdbFloatFormatting() => Assert.That(Xml(Field(6.5f, "run", typeof(float))), Is.EqualTo("<run>6.5</run>"));

    [Test]
    public void Double_SerializesAsText() => Assert.That(Xml(Field(2.5d, "weight", typeof(double))), Is.EqualTo("<weight>2.5</weight>"));

    // Sample: <castShadows>true</castShadows> / <decalModel>false</decalModel> (Geometry.xdb)
    [Test]
    public void BoolTrue_SerializesLowercase() =>
        Assert.That(Xml(Field(true, "castShadows", typeof(bool))), Is.EqualTo("<castShadows>true</castShadows>"));

    [Test]
    public void BoolFalse_SerializesLowercase() =>
        Assert.That(Xml(Field(false, "decalModel", typeof(bool))), Is.EqualTo("<decalModel>false</decalModel>"));

    // Sample: <headBoneName>Head</headBoneName> (AnimationProperties.xdb)
    [Test]
    public void String_SerializesAsText() => Assert.That(Xml(Field("Head", "headBoneName", typeof(string))), Is.EqualTo("<headBoneName>Head</headBoneName>"));

    // Empty strings collapse to an empty element (no text node).
    [Test]
    public void EmptyString_SerializesAsEmptyElement() => Assert.That(Xml(Field("", "name", typeof(string))), Is.EqualTo("<name />"));

    [Test]
    public void NullString_SerializesAsEmptyElement() => Assert.That(Xml(Field(null, "name", typeof(string))), Is.EqualTo("<name />"));

    // ---------------------------------------------------------------- [EnumRef] int fields

    // Generated structs keep int (the game representation); [EnumRef] maps the value to its name.
    [Test]
    public void EnumRefInt_SerializesMemberName() =>
        Assert.That(Xml(Serializer.SerializeObject(new EnumRefHolder(), "Root").Element("kind")), Is.EqualTo("<kind>CREATURE_KIND_HORIZONTAL</kind>"));

    // Values missing from the (incomplete) recovered enum fall back to the raw number.
    [Test]
    public void EnumRefInt_UnknownValue_SerializesRawNumber() => Assert.That(
        Xml(Serializer.SerializeObject(new EnumRefHolder(), "Root").Element("unknownKind")), Is.EqualTo("<unknownKind>42</unknownKind>"));

    // int[] with [EnumRef] wraps each element in <Item>, named when possible.
    [Test]
    public void EnumRefIntArray_WrapsNamedItemsInItem() => Assert.That(Xml(Serializer.SerializeObject(new EnumRefHolder(), "Root").Element("kinds")),
        Is.EqualTo("<kinds><Item>CREATURE_KIND_VERTICAL</Item><Item>CREATURE_KIND_HORIZONTAL</Item><Item>42</Item></kinds>"));

    // ---------------------------------------------------------------- Arrays

    // Sample: <shaderIndices><Item>0</Item>...</shaderIndices> (StaticWater.xdb)
    [Test]
    public void PrimitiveArray_WrapsElementsInItem() => Assert.That(Xml(Field(new[] { 0, 1, 2 }, "shaderIndices", typeof(int[]))),
        Is.EqualTo("<shaderIndices><Item>0</Item><Item>1</Item><Item>2</Item></shaderIndices>"));

    // Sample: <profiles><Item>vs_2_0</Item></profiles> (StaticWater.xdb)
    [Test]
    public void StringArray_WrapsElementsInItem() => Assert.That(Xml(Field(new[] { "vs_2_0" }, "profiles", typeof(string[]))),
        Is.EqualTo("<profiles><Item>vs_2_0</Item></profiles>"));

    // Sample: <objects /> (default.(VisualItem).xdb). Empty arrays are empty elements.
    [Test]
    public void EmptyArray_SerializesAsEmptyElement() =>
        Assert.That(Xml(Field(Array.Empty<int>(), "objects", typeof(int[]))), Is.EqualTo("<objects />"));

    [Test]
    public void NullArray_SerializesAsEmptyElement() => Assert.That(Xml(Field(null, "objects", typeof(int[]))), Is.EqualTo("<objects />"));

    // ---------------------------------------------------------------- File references

    // Sample: <binaryFile href="/Characters/Elf_female/ElfFemale.(Geometry).bin" /> (Geometry.xdb)
    [Test]
    public void FileRef_SerializesAsRootedHref() =>
        Assert.That(Xml(Field(new FileRef("Characters/Elf_female/ElfFemale.(Geometry).bin"), "binaryFile", typeof(FileRef))),
            Is.EqualTo("<binaryFile href=\"/Characters/Elf_female/ElfFemale.(Geometry).bin\" />"));

    [Test]
    public void EmptyFileRef_SerializesAsEmptyHref() => Assert.That(Xml(Field(new FileRef(""), "binaryFile", typeof(FileRef))),
        Is.EqualTo("<binaryFile href=\"\" />"));

    // TextFileRef: same href shape, pointing at a .txt resource.
    [Test]
    public void TextFileRef_SerializesAsRootedHref() =>
        Assert.That(Xml(Field(new TextFileRef("Texts/description.txt"), "description", typeof(TextFileRef))),
            Is.EqualTo("<description href=\"/Texts/description.txt\" />"));

    [Test]
    public void EmptyTextFileRef_SerializesAsEmptyHref() => Assert.That(Xml(Field(new TextFileRef(""), "description", typeof(TextFileRef))),
        Is.EqualTo("<description href=\"\" />"));

    // ---------------------------------------------------------------- ResourcePointer

    // Sample: href="/Material/userinfo.xdb#xpointer(/MaterialTemplate)" (typed pointer).
    [Test]
    public void ResourcePointer_WithType_SerializesXPointerHref() => Assert.That(
        Xml(Field(new ResourcePointer("Material/userinfo.xdb", typeof(SampleMaterial)), "surface", typeof(ResourcePointer))),
        Is.EqualTo("<surface href=\"/Material/userinfo.xdb#xpointer(/MaterialTemplate)\" />"));

    // Untyped pointer: just the rooted resource path.
    [Test]
    public void ResourcePointer_WithoutType_SerializesPlainHref() => Assert.That(
        Xml(Field(new ResourcePointer("Material/userinfo.xdb", null), "surface", typeof(ResourcePointer))),
        Is.EqualTo("<surface href=\"/Material/userinfo.xdb\" />"));

    // ---------------------------------------------------------------- NullablePointer (polymorphic)

    // Polymorphic pointer: concrete type name goes into a "type" attribute, fields nested inside.
    [Test]
    public void NullablePointer_SerializesConcreteTypeAndFields() => Assert.That(
        Xml(Field(new NullablePointer(new SampleParams()), "params", typeof(NullablePointer))),
        Is.EqualTo("<params type=\"CommonMaterialParams\"><intensity>5</intensity></params>"));

    // A null pointer produces no element at all (the field is skipped).
    [Test]
    public void NullNullablePointer_IsSkipped() => Assert.That(Field(NullablePointer.Empty, "params", typeof(NullablePointer)), Is.Null);

    // ---------------------------------------------------------------- Math types

    [Test]
    public void Vector2_SerializesAsXyAttributes() =>
        Assert.That(Xml(Field(new Vector2(1, 2), "uv", typeof(Vector2))), Is.EqualTo("<uv x=\"1\" y=\"2\" />"));

    // Sample: <center x="0.06831549" y="-0.260649949" z="0.693154752" /> (Geometry.xdb)
    [Test]
    public void Vector3_SerializesAsXyzAttributes() => Assert.That(Xml(Field(new Vector3(1, 2, 3), "center", typeof(Vector3))),
        Is.EqualTo("<center x=\"1\" y=\"2\" z=\"3\" />"));

    [Test]
    public void Quaternion_SerializesAsAttributes() => Assert.That(Xml(Field(new Quaternion(0, 0, 0, 1), "rotation", typeof(Quaternion))),
        Is.EqualTo("<rotation x=\"0\" y=\"0\" z=\"0\" w=\"1\" />"));

    [Test]
    public void BigVector3_SerializesAsXyzAttributes() => Assert.That(Xml(Field(new BigVector3(0, 0, 0, 1, 2, 3), "position", typeof(BigVector3))),
        Is.EqualTo("<position x=\"1\" y=\"2\" z=\"3\" />"));

    // ---------------------------------------------------------------- Nested struct

    // Sample: <aabb><center .../><extents .../></aabb> (Geometry.xdb). Nested types reflect their fields.
    [Test]
    public void NestedStruct_ReflectsFieldsRecursively() => Assert.That(
        Xml(Field(new AABB { center = new Vector3(1, 2, 3), extents = new Vector3(4, 5, 6) }, "aabb", typeof(AABB))),
        Is.EqualTo("<aabb><center x=\"1\" y=\"2\" z=\"3\" /><extents x=\"4\" y=\"5\" z=\"6\" /></aabb>"));

    // ---------------------------------------------------------------- Field naming & attributes

    // [XdbName] overrides the element name for a field.
    [Test]
    public void XdbName_OverridesFieldElementName() =>
        Assert.That(Serializer.SerializeObject(new RenamedHolder(), "Root").ToString(SaveOptions.DisableFormatting),
            Is.EqualTo("<Root><Name>Foo</Name></Root>"));

    // ---------------------------------------------------------------- Resource header & root name

    // Root element uses the type's [XdbName]; positive resource ids add a <Header> first.
    // Sample: <...VisualItem><Header><resourceId>564002825</resourceId></Header>... (default.(VisualItem).xdb)
    [Test]
    public void Resource_WithId_AddsHeaderAndUsesTypeXdbName()
    {
        var xml = XElement.Parse(Serializer.SerializeResource(new SampleResource(), 564002825)).ToString(SaveOptions.DisableFormatting);
        Assert.That(xml,
            Is.EqualTo("<gameMechanics.constructor.schemes.item.VisualItem>" + "<Header><resourceId>564002825</resourceId></Header>" +
                       "<dressSlot>3</dressSlot>" + "</gameMechanics.constructor.schemes.item.VisualItem>"));
    }

    // A non-positive resource id omits the header entirely.
    [Test]
    public void Resource_WithoutId_HasNoHeader()
    {
        var xml = XElement.Parse(Serializer.SerializeResource(new SampleResource(), 0)).ToString(SaveOptions.DisableFormatting);
        Assert.That(xml,
            Is.EqualTo("<gameMechanics.constructor.schemes.item.VisualItem>" + "<dressSlot>3</dressSlot>" +
                       "</gameMechanics.constructor.schemes.item.VisualItem>"));
    }
}
