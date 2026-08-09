using System.Xml.Linq;
using Microsoft.Extensions.Logging;

namespace AllodsOnlineEditorTools.ClientResources.Serialization.Xdb;

public class XdbStructSerializer(
    XdbStructSerializerOptions options,
    ResourceSerializationContext context,
    ILogger? logger = null)
    : StructSerializer<IXdbConverter, XElement, XElement>(options, context, logger)
{
    private static readonly XDeclaration XmlDeclaration = new("1.0", "UTF-8", null);

    public override string SerializeResource(object obj, int resourceId)
    {
        var xdb = SerializeObject(obj, XdbNameAttribute.Resolve(obj.GetType()));
        if (resourceId > 0)
        {
            var header = new XElement("Header", new XElement("resourceId", resourceId));
            xdb.AddFirst(header);
        }
        return XmlDeclaration + Environment.NewLine + xdb;
    }

    public override object ParseResource(string text, out int resourceId)
    {
        var root = XDocument.Parse(text).Root
            ?? throw new InvalidOperationException("xdb document has no root element");

        var idText = root.Element("Header")?.Element("resourceId")?.Value;
        resourceId = int.TryParse(idText, out var id) ? id : 0;

        return DeserializeObject(root, ResolveXdbType(root.Name.LocalName));
    }

    public XElement SerializeObject(object? obj, string name) => SerializeObjectNode(obj, name);

    public XElement? SerializeField(object? value, string name, Type type, Type? enumRef = null)
        => SerializeFieldNode(value, name, type, enumRef);

    public object DeserializeObject(XElement element, Type type) => DeserializeObjectNode(element, type);

    public object? DeserializeField(XElement element, Type type, Type? enumRef = null)
        => DeserializeFieldNode(element, type, enumRef);

    internal Type ResolveXdbType(string xdbName) => Context.ResolveByXdbName(xdbName);

    protected override XElement BeginObject(string name) => new(name);

    protected override void AddField(XElement objectNode, string name, XElement? child)
    {
        if (child is not null) objectNode.Add(child);
    }

    protected override XElement WriteNull(string name) => new(name);

    protected override XElement? WriteConverted(IXdbConverter converter, string name, object value)
        => converter.Write(this, name, value);

    protected override bool TryGetChild(XElement objectNode, string name, out XElement child)
    {
        var found = objectNode.Element(name);
        child = found!;
        return found is not null;
    }

    protected override string ReadScalarToken(XElement node) => node.Value;

    protected override IEnumerable<string> ReadItemTokens(XElement node) => node.Elements("Item").Select(item => item.Value);

    protected override object? ReadConverted(IXdbConverter converter, XElement node, Type type)
        => converter.Read(this, node, type);
}
