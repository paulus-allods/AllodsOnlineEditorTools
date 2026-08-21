using System.Xml.Linq;
using AllodsOnlineEditorTools.ClientResources.DataTypes;

namespace AllodsOnlineEditorTools.ClientResources.Serialization.Xdb.Converters;

internal class BigVector3XdbConverter : XdbConverter<BigVector3>
{
    protected override XElement WriteValue(XdbStructSerializer serializer, string elementName, BigVector3 value)
    {
        return new XElement(elementName, new XAttribute("x", value.X), new XAttribute("y", value.Y), new XAttribute("z", value.Z));
    }

    protected override BigVector3 ReadValue(XdbStructSerializer serializer, XElement element, Type type)
    {
        var value = default(BigVector3);
        value.X = XdbAttribute.Double(element, "x");
        value.Y = XdbAttribute.Double(element, "y");
        value.Z = XdbAttribute.Double(element, "z");
        return value;
    }
}
