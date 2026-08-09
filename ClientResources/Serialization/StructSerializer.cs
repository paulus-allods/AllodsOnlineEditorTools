using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AllodsOnlineEditorTools.ClientResources.Serialization;

public abstract class StructSerializer<TConverter, TWriteNode, TReadNode>(
    ConverterRegistry<TConverter> options,
    ResourceSerializationContext context,
    ILogger? logger) : IResourceWriter, IResourceReader
    where TConverter : class, ITypeConverter
{
    protected ResourceSerializationContext Context => context;
    public ILogger Logger => logger ?? NullLogger.Instance;

    public abstract string SerializeResource(object obj, int resourceId);
    public abstract object ParseResource(string text, out int resourceId);
    
    protected TWriteNode SerializeObjectNode(object? obj, string name)
    {
        var node = BeginObject(name);
        if (obj is null) return node;

        foreach (var field in StructModelCache.Get(obj.GetType()).Fields)
        {
            var child = SerializeFieldNode(field.GetValue(obj), field.XdbName, field.FieldType, context.ResolveEnumRef(field));
            AddField(node, field.XdbName, child);
        }

        return node;
    }

    protected TWriteNode? SerializeFieldNode(object? value, string name, Type type, Type? enumRef)
    {
        if (enumRef is not null && value is not null
            && EnumRefMaterializer.TryMaterialize(value, type, enumRef, out var token))
            return SerializeFieldNode(token, name, token!.GetType(), null);

        if (value is null) return WriteNull(name);

        var converter = options.GetConverter(type);
        if (converter is not null) return WriteConverted(converter, name, value);

        return SerializeObjectNode(value, name);
    }

    protected object DeserializeObjectNode(TReadNode node, Type type)
    {
        var obj = Activator.CreateInstance(type)
            ?? throw new InvalidOperationException($"Failed to create instance of {type.FullName}");

        foreach (var field in StructModelCache.Get(type).Fields)
            if (TryGetChild(node, field.XdbName, out var child))
                field.SetValue(obj, DeserializeFieldNode(child, field.FieldType, context.ResolveEnumRef(field)));

        return obj;
    }

    protected object? DeserializeFieldNode(TReadNode node, Type type, Type? enumRef)
    {
        if (IsNull(node)) return null;

        if (enumRef is not null && EnumRefMaterializer.TryDematerialize(
                type, enumRef, () => ReadScalarToken(node), () => ReadItemTokens(node), out var carrier))
            return carrier;

        var converter = options.GetConverter(type);
        return converter is not null ? ReadConverted(converter, node, type) : DeserializeObjectNode(node, type);
    }
    
    /// <summary>Creates the empty node an object serializes into (xdb: a named element; jdb: a dictionary).</summary>
    protected abstract TWriteNode BeginObject(string name);

    /// <summary>Attaches a field's serialized <paramref name="child"/> to its object node — xdb adds the
    /// self-named element (and skips a null/omitted one); jdb keys it under <paramref name="name"/>.</summary>
    protected abstract void AddField(TWriteNode objectNode, string name, TWriteNode? child);

    /// <summary>The node written for a null field value (xdb: an empty element; jdb: JSON null).</summary>
    protected abstract TWriteNode? WriteNull(string name);

    protected abstract TWriteNode? WriteConverted(TConverter converter, string name, object value);

    /// <summary>Reads the child node named <paramref name="name"/>; false when absent (leave the default).</summary>
    protected abstract bool TryGetChild(TReadNode objectNode, string name, out TReadNode child);

    /// <summary>Whether the node is an explicit null (jdb JSON null); xdb has none, hence the default.</summary>
    protected virtual bool IsNull(TReadNode node) => false;

    protected abstract string ReadScalarToken(TReadNode node);

    protected abstract IEnumerable<string> ReadItemTokens(TReadNode node);

    protected abstract object? ReadConverted(TConverter converter, TReadNode node, Type type);
}
