using AllodsOnlineEditorTools.ClientResources.Serialization.Jdb.Converters;

namespace AllodsOnlineEditorTools.ClientResources.Serialization.Jdb;

public class JdbStructSerializerOptions(bool prettyPrint) : ConverterRegistry<IJdbConverter>(DefaultConverters)
{
    private static readonly IReadOnlyList<IJdbConverter> DefaultConverters =
    [
        new ResourcePointerJdbConverter(),
        new FileRefJdbConverter(),
        new TextFileRefJdbConverter(),
        new WStringJdbConverter(),
        new NullablePointerJdbConverter(),
        new Vector2JdbConverter(),
        new Vector3JdbConverter(),
        new BigVector3JdbConverter(),
        new QuaternionJdbConverter(),
        new ArrayJdbConverter(),
        new PrimitivesJdbConverter(),
    ];

    public static JdbStructSerializerOptions Default { get; } = new(true);

    public bool PrettyPrint => prettyPrint;
}
