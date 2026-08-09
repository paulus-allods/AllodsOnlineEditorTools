using System.Numerics;
using System.Xml.Linq;

namespace AllodsOnlineEditorTools.ClientResources.Serialization.Xdb.Converters;

internal class QuaternionXdbConverter : XdbConverter<Quaternion>
{
    protected override XElement WriteValue(XdbStructSerializer serializer, string elementName, Quaternion value)
    {
        return new XElement(elementName,
            new XAttribute("x", XdbFloat.ToXdbString(value.X)),
            new XAttribute("y", XdbFloat.ToXdbString(value.Y)),
            new XAttribute("z", XdbFloat.ToXdbString(value.Z)),
            new XAttribute("w", XdbFloat.ToXdbString(value.W)));
    }

    protected override Quaternion ReadValue(XdbStructSerializer serializer, XElement element, Type type)
        => new(XdbAttribute.Float(element, "x"), XdbAttribute.Float(element, "y"),
            XdbAttribute.Float(element, "z"), XdbAttribute.Float(element, "w"));
}
