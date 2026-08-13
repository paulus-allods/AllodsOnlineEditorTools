// ReSharper disable InconsistentNaming
// ReSharper disable FieldCanBeMadeReadOnly.Global

namespace AllodsOnlineEditorTools.ClientResources.DataTypes;

/// <summary>
/// A "wide" string field whose binary payload is stored as UTF-16LE rather than the default single-byte/ASCII
/// encoding used by plain <see cref="string"/> fields.
/// </summary>
public struct WString(string value)
{
    public string Value = value;
}
