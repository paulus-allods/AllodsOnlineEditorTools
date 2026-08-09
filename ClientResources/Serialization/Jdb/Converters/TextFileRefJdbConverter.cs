using System.Text.Json;
using AllodsOnlineEditorTools.ClientResources.DataTypes;

namespace AllodsOnlineEditorTools.ClientResources.Serialization.Jdb.Converters;

internal class TextFileRefJdbConverter : JdbConverter<TextFileRef>
{
    protected override object WriteValue(JdbStructSerializer serializer, TextFileRef value)
        => new Dictionary<string, object?> { ["$href"] = value.Name };

    protected override TextFileRef ReadValue(JdbStructSerializer serializer, JsonElement element, Type type)
        => new(element.TryGetProperty("$href", out var href) ? href.GetString() ?? string.Empty : string.Empty);
}
