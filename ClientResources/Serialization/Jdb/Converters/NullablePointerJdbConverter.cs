using System.Text.Json;
using AllodsOnlineEditorTools.ClientResources.DataTypes;

namespace AllodsOnlineEditorTools.ClientResources.Serialization.Jdb.Converters;

internal class NullablePointerJdbConverter : JdbConverter<NullablePointer>
{
    protected override object? WriteValue(JdbStructSerializer serializer, NullablePointer value)
    {
        if (value.Value is null)
        {
            return null;
        }

        var document = new Dictionary<string, object?>
        {
            ["$type"] = value.Value.GetType().Name,
            ["$version"] = value.Value.GetType().Namespace ?? string.Empty,
        };
        foreach (var (key, fieldValue) in serializer.SerializeObject(value.Value))
        {
            document[key] = fieldValue;
        }

        return document;
    }

    protected override NullablePointer ReadValue(JdbStructSerializer serializer, JsonElement element, Type type)
        => new(serializer.DeserializeObject(element, serializer.ResolveDocumentType(element)));
}
