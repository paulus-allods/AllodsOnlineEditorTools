using System.Globalization;
using System.Xml.Linq;

namespace AllodsOnlineEditorTools.ClientResources.Serialization.Xdb.Converters;

internal static class XdbAttribute
{
    public static float Float(XElement element, string name)
        => float.Parse(element.Attribute(name)!.Value, CultureInfo.InvariantCulture);

    public static double Double(XElement element, string name)
        => double.Parse(element.Attribute(name)!.Value, CultureInfo.InvariantCulture);
}
