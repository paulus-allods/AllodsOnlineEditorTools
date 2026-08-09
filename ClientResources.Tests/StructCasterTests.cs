using AllodsOnlineEditorTools.ClientResources.DataTypes;
using AllodsOnlineEditorTools.ClientResources.Serialization;
using AllodsOnlineEditorTools.ClientResources.Serialization.Xdb;

// ReSharper disable UnusedMember.Local
// ReSharper disable InconsistentNaming
// ReSharper disable NotAccessedField.Local
#pragma warning disable CS0414 // Field is assigned but its value is never used
#pragma warning disable CS0649 // Field is never assigned to (target fixture fields are set by the caster via reflection)

namespace ClientResources.Tests;

/// <summary>
/// Validates cross-version casting: field projection between two fixture "versions",
/// enum remapping by entry name, <see cref="EnumRefAttribute.UseSourceOnCast"/> overrides,
/// and once-only warning emission.
/// </summary>
[TestFixture]
public class StructCasterTests
{
    private enum SourceRace { HUMAN = 0, ELF = 1, ORC = 2 }
    private enum TargetRace { ELF = 5, HUMAN = 10 } // same names, different values; no ORC

    private enum SourceAnimation { WALK = 0, RUN = 1 }
    private enum TargetAnimation { IDLE = 0 } // version-specific: names don't correspond

    private static class V1
    {
        public sealed class Mob
        {
            public int level = 3;
            public string name = "wolf";
            [EnumRef(typeof(SourceRace))] public int race = (int)SourceRace.ELF;
            [EnumRef(typeof(SourceRace))] public int[] allowedRaces = [0, 1, 2];
            [EnumRef(typeof(SourceAnimation))] public int animation = (int)SourceAnimation.RUN;
            public int legacyField = 7; // only exists in the source version
            public Stats stats = new();
        }

        public sealed class Stats
        {
            public int power = 11;
        }

        public sealed class OrphanStruct // no target counterpart
        {
            public int x = 1;
        }

        public sealed class NoEnumMob // simulates a version generated without types.xml
        {
            public int race = 2;
        }
    }

    private static class V2
    {
        public sealed class Mob
        {
            public int level;
            public string name = string.Empty;
            [EnumRef(typeof(TargetRace))] public int race;
            [EnumRef(typeof(TargetRace))] public int[] allowedRaces = [];
            [EnumRef(typeof(TargetAnimation), UseSourceOnCast = true)] public int animation;
            public int newField; // only exists in the target version
            public Stats stats = new();
        }

        public sealed class Stats
        {
            public int power;
        }

        public sealed class NoEnumMob
        {
            [EnumRef(typeof(TargetRace))] public int race;
        }
    }

    private static class RefV1
    {
        public sealed class Holder
        {
            public FileRef icon = new("icons/a.dds");
            public FileRef[] extras = [new("b.dds"), new("c.dds")];
        }
    }

    private static class RefV2
    {
        public sealed class Holder
        {
            public FileRef icon;
            public FileRef[] extras = [];
        }
    }

    private static class NestedArrV1
    {
        public sealed class Holder
        {
            public Item[] items = [new() { value = 1 }, new() { value = 2 }];
        }

        public sealed class Item
        {
            public int value;
            public int legacy = 9; // only in the source version
        }
    }

    private static class NestedArrV2
    {
        public sealed class Holder
        {
            public Item[] items = [];
        }

        public sealed class Item
        {
            public int value;
        }
    }
    
    private static (StructCaster Caster, CollectingLogger Logger) CreateCaster(params string[] structNames)
    {
        var sourceStructs = new Dictionary<string, Type>
        {
            ["Mob"] = typeof(V1.Mob),
            ["OrphanStruct"] = typeof(V1.OrphanStruct),
            ["NoEnumMob"] = typeof(V1.NoEnumMob),
        };
        var targetStructs = new Dictionary<string, Type>
        {
            ["Mob"] = typeof(V2.Mob),
            ["NoEnumMob"] = typeof(V2.NoEnumMob),
        };
        var logger = new CollectingLogger();
        var caster = new StructCaster(sourceStructs, targetStructs, logger);
        caster.Analyze(structNames);
        return (caster, logger);
    }
    
    private static StructCaster CreateCaster(IReadOnlyDictionary<string, Type> source, IReadOnlyDictionary<string, Type> target, params string[] names)
    {
        var caster = new StructCaster(source, target, new CollectingLogger());
        caster.Analyze(names);
        return caster;
    }

    [Test]
    public void MatchingFields_AreCopied()
    {
        var (caster, _) = CreateCaster("Mob");

        var result = (V2.Mob)caster.Cast(new V1.Mob());

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.level, Is.EqualTo(3));
            Assert.That(result.name, Is.EqualTo("wolf"));
        }
    }

    [Test]
    public void NestedStruct_IsCastRecursively()
    {
        var (caster, _) = CreateCaster("Mob");

        var result = (V2.Mob)caster.Cast(new V1.Mob());

        Assert.That(result.stats.power, Is.EqualTo(11));
    }

    [Test]
    public void SourceOnlyField_IsDropped_WithSingleWarning()
    {
        var (caster, logger) = CreateCaster("Mob", "Mob");

        caster.Cast(new V1.Mob());
        caster.Cast(new V1.Mob());

        Assert.That(logger.Warnings.Count(w => w.Contains("legacyField")), Is.EqualTo(1));
    }

    [Test]
    public void TargetOnlyField_IsLeftDefault_WithSingleWarning()
    {
        var (caster, logger) = CreateCaster("Mob", "Mob");

        var result = (V2.Mob)caster.Cast(new V1.Mob());

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.newField, Is.Zero);
            Assert.That(logger.Warnings.Count(w => w.Contains("newField")), Is.EqualTo(1));
        }
    }

    [Test]
    public void StructMissingFromTarget_IsNotCastable_WithSingleWarning()
    {
        var (caster, logger) = CreateCaster("OrphanStruct", "OrphanStruct");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(caster.CanCast("OrphanStruct"), Is.False);
            Assert.That(caster.IncompatibilityCount, Is.GreaterThanOrEqualTo(1));
            Assert.That(logger.Warnings.Count(w => w.Contains("OrphanStruct")), Is.EqualTo(1));
        }
    }

    [Test]
    public void Cast_WithoutPlan_Throws()
    {
        var (caster, _) = CreateCaster("Mob");

        Assert.Throws<InvalidOperationException>(() => caster.Cast(new V1.OrphanStruct()));
    }
    
    [Test]
    public void EnumField_RemapsByEntryName()
    {
        var (caster, _) = CreateCaster("Mob");

        var result = (V2.Mob)caster.Cast(new V1.Mob());

        // SourceRace.ELF (1) → TargetRace.ELF (5)
        Assert.That(result.race, Is.EqualTo((int)TargetRace.ELF));
    }

    [Test]
    public void EnumArrayField_RemapsElements()
    {
        var (caster, _) = CreateCaster("Mob");

        var result = (V2.Mob)caster.Cast(new V1.Mob());

        // HUMAN 0→10, ELF 1→5, ORC 2 has no counterpart → numeric value kept.
        Assert.That(result.allowedRaces, Is.EqualTo([10, 5, 2]));
    }

    [Test]
    public void EnumNameMiss_KeepsValue_WithSingleWarning()
    {
        var (caster, logger) = CreateCaster("Mob");

        caster.Cast(new V1.Mob());
        caster.Cast(new V1.Mob());

        Assert.That(logger.Warnings.Count(w => w.Contains("ORC") && w.Contains("no counterpart")), Is.EqualTo(1));
    }

    [Test]
    public void NoEnumSource_CopiesNumericValue()
    {
        var (caster, _) = CreateCaster("NoEnumMob");

        var result = (V2.NoEnumMob)caster.Cast(new V1.NoEnumMob());

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.race, Is.EqualTo(2));
            Assert.That(caster.EnumRefOverrides, Is.Empty);
        }
    }

    [Test]
    public void UseSourceOnCast_KeepsValue_AndRegistersSourceEnumOverride()
    {
        var (caster, _) = CreateCaster("Mob");

        var result = (V2.Mob)caster.Cast(new V1.Mob());

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.animation, Is.EqualTo((int)SourceAnimation.RUN));
            Assert.That(caster.EnumRefOverrides[(typeof(V2.Mob), nameof(V2.Mob.animation))], Is.EqualTo(typeof(SourceAnimation)));
        }
    }

    [Test]
    public void UseSourceOnCast_XdbSerializer_RendersSourceEnumName()
    {
        var (caster, _) = CreateCaster("Mob");
        var result = caster.Cast(new V1.Mob());

        var context = new ResourceSerializationContext { EnumRefOverrides = caster.EnumRefOverrides };
        var serializer = new XdbStructSerializer(XdbStructSerializerOptions.Default, context);
        var animation = serializer.SerializeObject(result, "Root").Element("animation");

        // TargetAnimation has no value 1; the override renders SourceAnimation.RUN instead.
        Assert.That(animation!.Value, Is.EqualTo(nameof(SourceAnimation.RUN)));
    }

    [Test]
    public void NestedStructArray_IsCastElementWise()
    {
        // items is Item[] where Item differs across versions (same name, different Type), so the array
        // branch must recurse into the nested-struct branch for each element.
        var caster = CreateCaster(
            new Dictionary<string, Type> { ["Holder"] = typeof(NestedArrV1.Holder) },
            new Dictionary<string, Type> { ["Holder"] = typeof(NestedArrV2.Holder) },
            "Holder");

        var result = (NestedArrV2.Holder)caster.Cast(new NestedArrV1.Holder());

        Assert.That(result.items.Select(i => i.value), Is.EqualTo([1, 2]));
    }

    [Test]
    public void FileRef_IsCopiedAcrossVersions()
    {
        // FileRef is one type across versions; the copy is handled by the identity branch.
        var caster = CreateCaster(
            new Dictionary<string, Type> { ["Holder"] = typeof(RefV1.Holder) },
            new Dictionary<string, Type> { ["Holder"] = typeof(RefV2.Holder) },
            "Holder");

        var result = (RefV2.Holder)caster.Cast(new RefV1.Holder());

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.icon.Name, Is.EqualTo("icons/a.dds"));
            Assert.That(result.extras.Select(e => e.Name), Is.EqualTo(["b.dds", "c.dds"]));
        }
    }
}
