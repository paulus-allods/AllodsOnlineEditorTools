using System.Globalization;
using System.Xml.Linq;

namespace AllodsOnlineEditorTools.ClientResources.Serialization.Xdb.Converters;

internal class PrimitivesXdbConverter : XdbConverter<object>
{
    public override bool CanConvert(Type type) => type.IsPrimitive || type == typeof(string);

    protected override XElement WriteValue(XdbStructSerializer serializer, string elementName, object? value)
    {
        return value switch
        {
            float f => new XElement(elementName, XdbFloat.ToXdbString(f)),
            string s => s.Length > 0 ? new XElement(elementName, s) : new XElement(elementName),
            _ => new XElement(elementName, value),
        };
    }

    protected override object ReadValue(XdbStructSerializer serializer, XElement element, Type type)
    {
        if (type == typeof(string))
        {
            return element.Value;
        }

        return Convert.ChangeType(element.Value, type, CultureInfo.InvariantCulture);
    }
}
