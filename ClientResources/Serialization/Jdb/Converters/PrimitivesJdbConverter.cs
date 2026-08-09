using System.Text.Json;

namespace AllodsOnlineEditorTools.ClientResources.Serialization.Jdb.Converters;

internal class PrimitivesJdbConverter : JdbConverter<object>
{
    public override bool CanConvert(Type type) => type.IsPrimitive || type == typeof(string);
    protected override object? WriteValue(JdbStructSerializer serializer, object value) => value;

    protected override object ReadValue(JdbStructSerializer serializer, JsonElement element, Type type)
        => element.Deserialize(type)!;
}
