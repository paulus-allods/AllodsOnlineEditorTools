namespace AllodsOnlineEditorTools.ClientResources.Serialization.Bin;

public interface IBinaryConverter : ITypeConverter
{
    object? Read(ref BinaryStructReader reader, int offset, Type typeToConvert, BinaryStructSerializerContext context);
    void Write(BinaryStructWriter writer, int offset, object? value, BinaryStructSerializerContext context);
    int GetSize(Type type, BinaryStructSerializerContext context);
}