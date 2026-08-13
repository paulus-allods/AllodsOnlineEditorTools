using System.Xml.Linq;

namespace AllodsOnlineEditorTools.ClientResources.Serialization.Xdb.Converters;

internal class ArrayXdbConverter : XdbConverter<Array>
{
    public override bool CanConvert(Type type) => type.IsArray;

    protected override XElement WriteValue(XdbStructSerializer serializer, string elementName, Array? value)
    {
        var elementType = value?.GetType().GetElementType();
        var root = new XElement(elementName);
        if (value is null)
        {
            return root;
        }

        // Dispatch on each item's runtime type so heterogeneous token arrays (e.g. [EnumRef] name/number
        // mixes) serialize element-wise; for uniform arrays this is the declared element type anyway.
        foreach (var item in value)
        {
            root.Add(serializer.SerializeField(item, "Item", item?.GetType() ?? elementType!));
        }

        return root;
    }

    protected override Array ReadValue(XdbStructSerializer serializer, XElement element, Type type)
    {
        var elementType = type.GetElementType()!;
        var items = element.Elements("Item").ToList();
        var array = Array.CreateInstance(elementType, items.Count);
        for (var i = 0; i < items.Count; i++)
        {
            array.SetValue(serializer.DeserializeField(items[i], elementType), i);
        }

        return array;
    }
}
