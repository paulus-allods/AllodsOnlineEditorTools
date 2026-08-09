using AllodsOnlineEditorTools.ClientResources.Serialization.Xdb.Converters;

namespace AllodsOnlineEditorTools.ClientResources.Serialization.Xdb;

public class XdbStructSerializerOptions() : ConverterRegistry<IXdbConverter>(DefaultConverters)
{
    private static readonly IReadOnlyList<IXdbConverter> DefaultConverters =
    [
        new ResourcePointerXdbConverter(),
        new FileRefXdbConverter(),
        new TextFileRefXdbConverter(),
        new WStringXdbConverter(),
        new NullablePointerXdbConverter(),
        new Vector2XdbConverter(),
        new Vector3XdbConverter(),
        new BigVector3XdbConverter(),
        new QuaternionXdbConverter(),
        new ArrayXdbConverter(),
        new PrimitivesXdbConverter(),
    ];

    public static XdbStructSerializerOptions Default { get; } = new();
}
