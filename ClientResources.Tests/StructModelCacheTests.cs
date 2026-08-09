using AllodsOnlineEditorTools.ClientResources.Serialization;
using AllodsOnlineEditorTools.ClientResources.Serialization.Bin;
using AllodsOnlineEditorTools.ClientResources.Serialization.Xdb;

// ReSharper disable UnusedMember.Local
// ReSharper disable InconsistentNaming
#pragma warning disable CS0414 // Field is assigned but its value is never used

namespace ClientResources.Tests;

/// <summary>
/// Validates the single reflected struct view: field enumeration, [XdbName] naming (defaulting to
/// the field name), [EnumRef] capture, nullable [FieldOffset], and per-type caching.
/// </summary>
[TestFixture]
public class StructModelCacheTests
{
    private enum Race { HUMAN, ELF }

    [XdbName("RenamedStruct")]
    private sealed class Sample
    {
        public int plain = 1;
        [XdbName("Renamed")] public int renamed = 2;
        [EnumRef(typeof(Race))] public int race = 0;
        [FieldOffset(8)] public int located = 3;
    }

    private static StructField FieldNamed(string name) =>
        StructModelCache.Get(typeof(Sample)).Fields.Single(f => f.Name == name);

    [Test]
    public void TypeXdbName_UsesXdbNameAttribute() =>
        Assert.That(StructModelCache.Get(typeof(Sample)).XdbName, Is.EqualTo("RenamedStruct"));

    [Test]
    public void AllPublicInstanceFields_AreModelled() =>
        Assert.That(StructModelCache.Get(typeof(Sample)).Fields.Select(f => f.Name),
            Is.EquivalentTo(["plain", "renamed", "race", "located"]));

    [Test]
    public void FieldXdbName_DefaultsToFieldName() =>
        Assert.That(FieldNamed("plain").XdbName, Is.EqualTo("plain"));

    [Test]
    public void FieldXdbName_UsesXdbNameAttributeWhenPresent() =>
        Assert.That(FieldNamed("renamed").XdbName, Is.EqualTo("Renamed"));

    [Test]
    public void EnumRef_IsCaptured() =>
        Assert.That(FieldNamed("race").EnumRef, Is.EqualTo(typeof(Race)));

    [Test]
    public void EnumRef_IsNullWhenAbsent() =>
        Assert.That(FieldNamed("plain").EnumRef, Is.Null);

    [Test]
    public void Offset_IsCapturedFromAttribute() =>
        Assert.That(FieldNamed("located").Offset, Is.EqualTo(8));

    [Test]
    public void Offset_IsNullWhenAbsent() =>
        Assert.That(FieldNamed("plain").Offset, Is.Null);

    [Test]
    public void Get_ReturnsCachedInstance()
    {
        var first = StructModelCache.Get(typeof(Sample));
        var second = StructModelCache.Get(typeof(Sample));
        Assert.That(second, Is.SameAs(first));
    }
}
