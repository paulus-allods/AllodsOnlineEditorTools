using System.Diagnostics;
using System.Reflection;
using System.Text.RegularExpressions;

namespace AllodsOnlineEditorTools.ClientResources.Serialization.Bin;

internal static partial class FieldValidator
{
    private const string NoSource = "NO_SOURCE";

    private static readonly string[] FileRefExtensions =
        [".bin", ".tga", ".bev", ".bsb", ".fsb", ".psd", ".lua", ".cur", ".ttf", ".hlsl", ".eh", ".ej", ".ogv"];

    private static readonly string[] PlainStringExemptions = [".pak", ".xdb"];

    // A file-extension-looking suffix: a dot followed by 2-3 non-digit characters at the end of the string.
    [GeneratedRegex(@"\.[^\d.]{2,3}$")]
    private static partial Regex FileExtensionRegex();

    [Conditional("DEBUG")]
    public static void ValidatePlainString(string value, long offset)
    {
        Debug.Assert(!FileExtensionRegex().IsMatch(value) || PlainStringExemptions.Any(value.EndsWith),
            $"String field at offset {offset} contains '{value}', which looks like a file reference; the field is probably a FileRef/TextFileRef wrongly marked as string");
    }

    [Conditional("DEBUG")]
    public static void ValidateFileRef(string value, long offset)
    {
        Debug.Assert(
            value.Length == 0 || value.Contains(NoSource, StringComparison.InvariantCulture) ||
            FileRefExtensions.Any(value.EndsWith),
            $"FileRef field at offset {offset} contains '{value}', which does not end with a known file extension ({string.Join(", ", FileRefExtensions)}); the field is probably not a FileRef");
    }

    [Conditional("DEBUG")]
    public static void ValidateTextFileRef(string value, long offset)
    {
        Debug.Assert(value.Length == 0 || value.EndsWith(".txt", StringComparison.InvariantCulture),
            $"TextFileRef field at offset {offset} contains '{value}', which does not end with .txt; the field is probably not a TextFileRef");
    }

    [Conditional("DEBUG")]
    public static void ValidateEnumRef(FieldInfo field, long offset, Type enumRef, object? value)
    {
        switch (value)
        {
            case int intValue:
                Debug.Assert(Enum.IsDefined(enumRef, intValue),
                    $"Field '{field.DeclaringType?.Name}.{field.Name}' at offset {offset} contains {intValue}, which is not defined in {enumRef.Name}; the enum definition is probably incomplete or the field is not a {enumRef.Name}");
                break;
            case int[] intValues:
                foreach (var item in intValues)
                {
                    Debug.Assert(Enum.IsDefined(enumRef, item),
                        $"Field '{field.DeclaringType?.Name}.{field.Name}' at offset {offset} contains {item}, which is not defined in {enumRef.Name}; the enum definition is probably incomplete or the field is not a {enumRef.Name}");
                }

                break;
        }
    }
}
