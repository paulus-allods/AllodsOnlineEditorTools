using System.Text.Json;
using AllodsOnlineEditorTools.ClientResources.DataTypes;

namespace AllodsOnlineEditorTools.ClientResources.Serialization.Jdb.Converters;

internal class ResourcePointerJdbConverter : JdbConverter<ResourcePointer>
{
    // jdb references point at the sibling .jdb file (extension rewritten); the xpointer type is dropped.
    protected override object WriteValue(JdbStructSerializer serializer, ResourcePointer value)
        => new Dictionary<string, object?> { ["$href"] = Path.ChangeExtension(value.Href, ".jdb") };

    protected override ResourcePointer ReadValue(JdbStructSerializer serializer, JsonElement element, Type type)
        => new(element.TryGetProperty("$href", out var href) ? href.GetString() ?? string.Empty : string.Empty, null);
}
