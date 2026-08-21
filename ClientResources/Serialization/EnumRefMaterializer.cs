namespace AllodsOnlineEditorTools.ClientResources.Serialization;

/// <summary>
/// Turns the game's raw <c>int</c> / <c>int[]</c> representation of an <c>[EnumRef]</c> field into display
/// token(s) and back. A token is the enum member name, or the raw number when the value is absent from the
/// (possibly incomplete) recovered enum. Formats serialize the tokens with their ordinary string/number
/// handling, so no format needs enum-specific code.
/// </summary>
public static class EnumRefMaterializer
{
    /// <summary>
    /// When <paramref name="fieldType"/> is the <c>int</c>/<c>int[]</c> carrier of an enum-ref field, writes
    /// its display token(s) into <paramref name="materialized"/> and returns true; otherwise returns false.
    /// Each token is the member name (a <c>string</c>) when the value is defined, or the raw <c>int</c> when
    /// it is not, kept as an <c>int</c> so numeric formats render it as a number. An array yields an
    /// <c>object[]</c> of such tokens, each carrying its own runtime type.
    /// </summary>
    public static bool TryMaterialize(object? value, Type fieldType, Type enumType, out object? materialized)
    {
        if (fieldType == typeof(int))
        {
            materialized = EnumToken((int)value!, enumType);
            return true;
        }

        if (fieldType == typeof(int[]))
        {
            var ints = (int[])value!;
            var tokens = new object[ints.Length];
            for (var i = 0; i < ints.Length; i++)
            {
                tokens[i] = EnumToken(ints[i], enumType);
            }

            materialized = tokens;
            return true;
        }

        materialized = null;
        return false;
    }

    private static object EnumToken(int value, Type enumType) =>
        Enum.GetName(enumType, value) is { } name ? name : value;

    /// <summary>
    /// The inverse of <see cref="TryMaterialize"/>: when <paramref name="fieldType"/> is the
    /// <c>int</c>/<c>int[]</c> carrier of an enum-ref field, reads the materialized token(s) (enum names,
    /// or raw numbers for values missing from the enum) back into the underlying <c>int</c>/<c>int[]</c>
    /// carrier. The caller supplies <paramref name="readScalar"/> (the single-value case) and
    /// <paramref name="readItems"/> (the array case) as token text; only the one matching
    /// <paramref name="fieldType"/> is invoked, keeping this method free of any format-specific element type.
    /// </summary>
    public static bool TryDematerialize(Type fieldType, Type enumType, Func<string> readScalar, Func<IEnumerable<string>> readItems, out object? carrier)
    {
        if (fieldType == typeof(int))
        {
            carrier = ParseEnumInt(readScalar(), enumType);
            return true;
        }

        if (fieldType == typeof(int[]))
        {
            carrier = readItems().Select(token => ParseEnumInt(token, enumType)).ToArray();
            return true;
        }

        carrier = null;
        return false;
    }

    private static int ParseEnumInt(string token, Type enumType) =>
        int.TryParse(token, out var number) ? number : Convert.ToInt32(Enum.Parse(enumType, token));
}
