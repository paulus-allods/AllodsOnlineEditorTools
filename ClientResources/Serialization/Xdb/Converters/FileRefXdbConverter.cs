using System.Xml.Linq;
using AllodsOnlineEditorTools.ClientResources.DataTypes;

namespace AllodsOnlineEditorTools.ClientResources.Serialization.Xdb.Converters;

internal class FileRefXdbConverter : XdbConverter<FileRef>
{
    protected override XElement WriteValue(XdbStructSerializer serializer, string elementName, FileRef value)
    {
        var href = string.IsNullOrEmpty(value.Name) ? "" : $"/{value.Name}";
        return new XElement(elementName, new XAttribute("href", href));
    }

    protected override FileRef ReadValue(XdbStructSerializer serializer, XElement element, Type type)
    {
        var href = element.Attribute("href")?.Value ?? string.Empty;
        return new FileRef(href.StartsWith('/') ? href[1..] : href);
    }
}
