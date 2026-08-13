// ReSharper disable InconsistentNaming
// ReSharper disable FieldCanBeMadeReadOnly.Global

namespace AllodsOnlineEditorTools.ClientResources.DataTypes;

public struct ResourcePointer(string href, Type? type)
{
    public static readonly ResourcePointer Empty = new(string.Empty, null);

    public string Href = href;
    public Type? Type = type;
}
