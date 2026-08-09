using System.Reflection;
using AllodsOnlineEditorTools.ClientResources.Serialization.Bin.Converters;

namespace AllodsOnlineEditorTools.ClientResources.Serialization.Bin;

public class BinarySerializerOptions() : ConverterRegistry<IBinaryConverter>(DefaultConverters)
{
    private static readonly IReadOnlyList<IBinaryConverter> DefaultConverters =
    [
        new ArrayBinaryConverter(),
        new BigVector3BinaryConverter(),
        new FileRefBinaryConverter(),
        new NullablePointerBinaryConverter(),
        new PrimitivesBinaryConverter(),
        new QuaternionBinaryConverter(),
        new ResourcePointerBinaryConverter(),
        new TextFileRefBinaryConverter(),
        new WStringBinaryConverter(),
        new Vector2BinaryConverter(),
        new Vector3BinaryConverter(),
    ];
    
    public static BinarySerializerOptions Default { get; } = new();
    
    public int GetTypeSize(Type type, BinaryStructSerializerContext context)
    {
        var converter = GetConverter(type);
        if (converter is not null) return converter.GetSize(type, context);
        var sizeAttribute = type.GetCustomAttribute<StructSizeAttribute>();
        return sizeAttribute?.Size ?? throw new InvalidOperationException($"Cannot get size of type '{type.Name}': no converter matches and no {nameof(StructSizeAttribute)} is present");
    }
}
