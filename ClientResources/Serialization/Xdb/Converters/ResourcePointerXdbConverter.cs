using System.Diagnostics;
using System.Xml.Linq;
using AllodsOnlineEditorTools.ClientResources.DataTypes;
using Microsoft.Extensions.Logging;

namespace AllodsOnlineEditorTools.ClientResources.Serialization.Xdb.Converters;

internal class ResourcePointerXdbConverter : XdbConverter<ResourcePointer>
{
    protected override XElement? WriteValue(XdbStructSerializer serializer, string elementName, ResourcePointer value)
    {
        if (Equals(value, ResourcePointer.Empty))
        {
            return null;
        }

        if (value.Type is null)
        {
            serializer.Logger.LogWarning(
                "Resource pointer to {Href} has no resolved type; writing href without an xpointer", value.Href);
        }

        var typeName = value.Type is not null
            ? XdbNameAttribute.Resolve(value.Type)
            : string.Empty;
        var href = string.IsNullOrEmpty(typeName)
            ? $"/{value.Href}"
            : $"/{value.Href}#xpointer(/{typeName})";
        return new XElement(elementName, new XAttribute("href", href));
    }

    protected override ResourcePointer ReadValue(XdbStructSerializer serializer, XElement element, Type type)
    {
        var href = element.Attribute("href")?.Value ?? string.Empty;
        if (href.StartsWith('/'))
        {
            href = href[1..];
        }

        var xpointer = href.IndexOf('#');
        if (xpointer >= 0)
        {
            href = href[..xpointer];
        }

        return new ResourcePointer(href, null);
    }
}
