namespace AllodsOnlineEditorTools.ClientResources.Serialization;

/// <summary>
/// Marks an <c>int</c>/<c>int[]</c> field as a reference to <paramref name="enumType"/>: the game stores the
/// raw number, and this restores the enum so writers can render it by name (see <see cref="EnumRefMaterializer"/>).
/// </summary>
[AttributeUsage(AttributeTargets.Field)]
public class EnumRefAttribute(Type enumType) : Attribute
{
    public Type EnumType { get; } = enumType;

    /// <summary>
    /// Keep the source version's value and enum on a cross-version cast instead of remapping by entry name.
    /// Set it when the target version's enum names don't correspond to the source's, so name-based remapping
    /// would be wrong; the field is then written using the source enum.
    /// </summary>
    public bool UseSourceOnCast { get; init; }
}
