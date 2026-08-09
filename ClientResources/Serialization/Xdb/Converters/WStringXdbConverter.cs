using System.Xml.Linq;
using AllodsOnlineEditorTools.ClientResources.DataTypes;

namespace AllodsOnlineEditorTools.ClientResources.Serialization.Xdb.Converters;

internal class WStringXdbConverter : XdbConverter<WString>
{
    protected override XElement WriteValue(XdbStructSerializer serializer, string elementName, WString value)
    {
        return value.Value is { Length: > 0 } text ? new XElement(elementName, text) : new XElement(elementName);
    }

    protected override WString ReadValue(XdbStructSerializer serializer, XElement element, Type type)
        => new(element.Value);
}