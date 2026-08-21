using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace AllodsOnlineEditorTools.ClientResources.Serialization.Jdb;

public class JdbStructSerializer(JdbStructSerializerOptions options, ResourceSerializationContext context, ILogger? logger = null)
    : StructSerializer<IJdbConverter, object, JsonElement>(options, context, logger)
{
    private readonly JsonSerializerOptions _writeOptions = new() { WriteIndented = options.PrettyPrint };

    public override string SerializeResource(object obj, int resourceId)
    {
        var document = new Dictionary<string, object?>();
        if (resourceId > 0)
        {
            document["$resourceId"] = resourceId;
        }

        document["$type"] = obj.GetType().Name;
        document["$version"] = obj.GetType().Namespace ?? string.Empty;

        foreach (var (key, value) in SerializeObject(obj))
        {
            document[key] = value;
        }

        return JsonSerializer.Serialize(document, _writeOptions);
    }

    public override object ParseResource(string text, out int resourceId)
    {
        using var doc = JsonDocument.Parse(text);
        var root = doc.RootElement;

        resourceId = root.TryGetProperty("$resourceId", out var idElement) && idElement.ValueKind == JsonValueKind.Number ? idElement.GetInt32() : 0;

        return DeserializeObject(root, ResolveDocumentType(root));
    }

    public Dictionary<string, object?> SerializeObject(object obj) =>
        (Dictionary<string, object?>)SerializeObjectNode(obj, "");

    public object? SerializeField(object? value, Type type, Type? enumRef) =>
        SerializeFieldNode(value, "", type, enumRef);

    public object DeserializeObject(JsonElement element, Type type) => DeserializeObjectNode(element, type);

    public object? DeserializeField(JsonElement element, Type type, Type? enumRef) =>
        DeserializeFieldNode(element, type, enumRef);

    internal Type ResolveDocumentType(JsonElement element)
    {
        if (!element.TryGetProperty("$type", out var typeElement))
        {
            throw new InvalidOperationException("JSON object missing $type field");
        }

        if (!element.TryGetProperty("$version", out var versionElement))
        {
            throw new InvalidOperationException("JSON object missing $version field");
        }

        var typeName = typeElement.GetString() ?? throw new InvalidOperationException("$type field is null");
        var version = versionElement.GetString() ?? throw new InvalidOperationException("$version field is null");
        return Context.ResolveDocumentType(version, typeName);
    }

    protected override object BeginObject(string name) => new Dictionary<string, object?>();

    protected override void AddField(object objectNode, string name, object? child) =>
        ((Dictionary<string, object?>)objectNode)[name] = child;

    protected override object? WriteNull(string name) => null;

    protected override object? WriteConverted(IJdbConverter converter, string name, object value) =>
        converter.Write(this, value);

    protected override bool TryGetChild(JsonElement objectNode, string name, out JsonElement child) =>
        objectNode.TryGetProperty(name, out child);

    protected override bool IsNull(JsonElement node) => node.ValueKind == JsonValueKind.Null;

    protected override string ReadScalarToken(JsonElement node) => node.ToString();

    protected override IEnumerable<string> ReadItemTokens(JsonElement node) =>
        node.EnumerateArray().Select(item => item.ToString());

    protected override object? ReadConverted(IJdbConverter converter, JsonElement node, Type type) =>
        converter.Read(this, node, type);
}
