// ReSharper disable InconsistentNaming
// ReSharper disable FieldCanBeMadeReadOnly.Global

namespace AllodsOnlineEditorTools.ClientResources.DataTypes;

public struct NullablePointer(object? value)
{
    public static readonly NullablePointer Empty = new(null);

    public object? Value = value;
}
