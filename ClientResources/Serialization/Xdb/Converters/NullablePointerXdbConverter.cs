using System.Xml.Linq;
using AllodsOnlineEditorTools.ClientResources.DataTypes;

namespace AllodsOnlineEditorTools.ClientResources.Serialization.Xdb.Converters;

internal class NullablePointerXdbConverter : XdbConverter<NullablePointer>
{
    protected override XElement? WriteValue(XdbStructSerializer serializer, string elementName, NullablePointer value)
    {
        if (value.Value is null) return null;
        var innerObj = value.Value;
        var concreteTypeName = XdbNameAttribute.Resolve(innerObj.GetType());
        var element = serializer.SerializeObject(innerObj, elementName);
        element.SetAttributeValue("type", concreteTypeName);
        return element;
    }

    protected override NullablePointer ReadValue(XdbStructSerializer serializer, XElement element, Type type)
    {
        var typeName = element.Attribute("type")?.Value
            ?? throw new InvalidOperationException("NullablePointer element is missing its 'type' attribute");
        var concreteType = serializer.ResolveXdbType(typeName);
        return new NullablePointer(serializer.DeserializeObject(element, concreteType));
    }
}
