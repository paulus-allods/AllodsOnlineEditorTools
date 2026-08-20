using System.Reflection;
using AllodsOnlineEditorTools.ClientResources.Serialization;
using AllodsOnlineEditorTools.ClientResources.Serialization.Bin;
using AllodsOnlineEditorTools.ClientResources.Structs;

namespace ClientResources.Tests;

/// <summary>
/// Validates that every generated struct's [FieldOffset] layout is internally consistent: fields fit
/// within their declared slots (no overlap) and nested structs recurse the same check. Runs across
/// every game version that ships struct definitions.
/// </summary>
[TestFixture]
public class StructLayoutValidation
{
    // One case per (version, struct type), across every version that has generated structs. Struct
    // sizing depends on the version's FileRefKind, so the version travels with the type.
    private static IEnumerable<TestCaseData> StructCases()
    {
        foreach (var version in GameVersion.Versions.Values)
        {
            foreach (var type in LoadStructs(version))
            {
                yield return new TestCaseData(version, type).SetArgDisplayNames(version.Name, type.Name);
            }
        }
    }

    private static IEnumerable<Type> LoadStructs(GameVersion version)
        => StructTypeResolver.FromVersion(version).Types;

    [TestCaseSource(nameof(StructCases))]
    public void ValidateStruct(GameVersion version, Type type)
    {
        var context = TestContexts.TestContext(version.FileRefKind);
        ValidateClass(context, type, 0, SizeOf(context, type));
    }

    private static int SizeOf(BinaryStructSerializerContext context, Type type) =>
        BinarySerializerOptions.Default.GetTypeSize(type, context);

    private void ValidateClass(BinaryStructSerializerContext context, Type type, int baseOffset, int endOffset)
    {
        var orderedFields = type.GetFields()
            .Select(f => (Field: f, f.GetCustomAttribute<FieldOffsetAttribute>()?.Offset))
            .Where(f => f.Offset is not null)
            .OrderBy(f => f.Offset!.Value)
            .Select(f => f.Field)
            .ToArray();

        var nestedStructs = type.GetNestedTypes().Where(c => c.GetCustomAttribute<StructSizeAttribute>() is not null);

        using (Assert.EnterMultipleScope())
        {
            foreach (var nestedStruct in nestedStructs)
            {
                ValidateClass(context, nestedStruct, 0, SizeOf(context, nestedStruct));
            }

            for (var i = 0; i < orderedFields.Length; i++)
            {
                var nextOffset = i == orderedFields.Length - 1
                    ? endOffset
                    : baseOffset + orderedFields[i + 1].GetCustomAttribute<FieldOffsetAttribute>()!.Offset;
                ValidateField(context, orderedFields[i], baseOffset, nextOffset);
            }
        }
    }

    private void ValidateField(BinaryStructSerializerContext context, FieldInfo field, int baseOffset, int nextOffset)
    {
        var fieldOffsetAnnotation = field.GetCustomAttribute<FieldOffsetAttribute>();
        if (fieldOffsetAnnotation is null)
        {
            return;
        }

        if (field.FieldType.IsClass && field.FieldType != typeof(string))
        {
            ValidateClass(context, field.FieldType, baseOffset + fieldOffsetAnnotation.Offset, nextOffset);
        }
        else
        {
            var size = SizeOf(context, field.FieldType);
            var room = nextOffset - (baseOffset + fieldOffsetAnnotation.Offset + size);
            if (room < 0)
            {
                Assert.Fail(
                    $"Field {field.Name} of type {field.FieldType} in {field.DeclaringType} has extra {-room} space");
            }

            if (room > 0 && field.FieldType != typeof(bool)) // Booleans are aligned on 1 byte
            {
                Assert.Warn(
                    $"Field {field.Name} of type {field.FieldType} in {field.DeclaringType} has extra {room} space");
            }
        }
    }
}
