using System.Text.Json;

namespace AllodsOnlineEditorTools.ClientResources.Serialization.Jdb.Converters;

internal class ArrayJdbConverter : JdbConverter<Array>
{
    public override bool CanConvert(Type type) => type.IsArray;

    protected override object WriteValue(JdbStructSerializer serializer, Array value)
    {
        var elementType = value.GetType().GetElementType()!;
        var items = new List<object?>(value.Length);
        // Dispatch on each item's runtime type so heterogeneous token arrays (e.g. [EnumRef] name/number
        // mixes) serialize element-wise; for uniform arrays this is the declared element type anyway.
        items.AddRange(from object? item in value select serializer.SerializeField(item, item?.GetType() ?? elementType, null));
        return items;
    }

    protected override Array ReadValue(JdbStructSerializer serializer, JsonElement element, Type type)
    {
        var elementType = type.GetElementType()!;
        var items = element.EnumerateArray().ToArray();
        var array = Array.CreateInstance(elementType, items.Length);
        for (var i = 0; i < items.Length; i++)
            array.SetValue(serializer.DeserializeField(items[i], elementType, null), i);
        return array;
    }
}
