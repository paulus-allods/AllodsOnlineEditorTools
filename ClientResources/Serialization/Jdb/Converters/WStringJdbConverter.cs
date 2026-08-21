using System.Text.Json;
using AllodsOnlineEditorTools.ClientResources.DataTypes;

namespace AllodsOnlineEditorTools.ClientResources.Serialization.Jdb.Converters;

internal class WStringJdbConverter : JdbConverter<WString>
{
    protected override object WriteValue(JdbStructSerializer serializer, WString value) => value.Value ?? string.Empty;

    protected override WString ReadValue(JdbStructSerializer serializer, JsonElement element, Type type) =>
        new(element.GetString() ?? string.Empty);
}
