using System.Numerics;
using System.Xml.Linq;

namespace AllodsOnlineEditorTools.ClientResources.Serialization.Xdb.Converters;

internal class Vector2XdbConverter : XdbConverter<Vector2>
{
    protected override XElement WriteValue(XdbStructSerializer serializer, string elementName, Vector2 value)
    {
        return new XElement(elementName, new XAttribute("x", XdbFloat.ToXdbString(value.X)), new XAttribute("y", XdbFloat.ToXdbString(value.Y)));
    }

    protected override Vector2 ReadValue(XdbStructSerializer serializer, XElement element, Type type) =>
        new(XdbAttribute.Float(element, "x"), XdbAttribute.Float(element, "y"));
}
