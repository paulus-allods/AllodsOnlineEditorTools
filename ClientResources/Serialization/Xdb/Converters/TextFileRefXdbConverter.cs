using System.Xml.Linq;
using AllodsOnlineEditorTools.ClientResources.DataTypes;

namespace AllodsOnlineEditorTools.ClientResources.Serialization.Xdb.Converters;

internal class TextFileRefXdbConverter : XdbConverter<TextFileRef>
{
    protected override XElement WriteValue(XdbStructSerializer serializer, string elementName, TextFileRef value)
    {
        var href = string.IsNullOrEmpty(value.Name) ? "" : $"/{value.Name}";
        return new XElement(elementName, new XAttribute("href", href));
    }

    protected override TextFileRef ReadValue(XdbStructSerializer serializer, XElement element, Type type)
    {
        var href = element.Attribute("href")?.Value ?? string.Empty;
        return new TextFileRef(href.StartsWith('/') ? href[1..] : href);
    }
}
