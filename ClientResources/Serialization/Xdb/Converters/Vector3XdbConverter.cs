using System.Numerics;
using System.Xml.Linq;

namespace AllodsOnlineEditorTools.ClientResources.Serialization.Xdb.Converters;

internal class Vector3XdbConverter : XdbConverter<Vector3>
{
    protected override XElement WriteValue(XdbStructSerializer serializer, string elementName, Vector3 value)
    {
        return new XElement(elementName, new XAttribute("x", XdbFloat.ToXdbString(value.X)), new XAttribute("y", XdbFloat.ToXdbString(value.Y)),
            new XAttribute("z", XdbFloat.ToXdbString(value.Z)));
    }

    protected override Vector3 ReadValue(XdbStructSerializer serializer, XElement element, Type type) =>
        new(XdbAttribute.Float(element, "x"), XdbAttribute.Float(element, "y"), XdbAttribute.Float(element, "z"));
}
