namespace AllodsOnlineEditorTools.ClientResources.Serialization.Bin;

public interface IBinaryConverter : ITypeConverter
{
    object? Read(ref BinaryStructReader reader, long offset, Type typeToConvert, BinaryStructSerializerContext context);
    void Write(BinaryStructWriter writer, long offset, object? value, BinaryStructSerializerContext context);
    int GetSize(Type type, BinaryStructSerializerContext context);
}
