namespace AllodsOnlineEditorTools.ClientResources.Serialization.Bin;

internal abstract class BinaryConverter<T> : IBinaryConverter
{
    protected abstract T ReadValue(ref BinaryStructReader reader, long offset, Type typeToConvert, BinaryStructSerializerContext context);

    protected abstract void WriteValue(BinaryStructWriter writer, long offset, T? value, BinaryStructSerializerContext context);

    public abstract int GetSize(Type type, BinaryStructSerializerContext context);

    public virtual bool CanConvert(Type type)
    {
        return type == typeof(T);
    }

    public object? Read(ref BinaryStructReader reader, long offset, Type typeToConvert,
        BinaryStructSerializerContext context)
    {
        return ReadValue(ref reader, offset, typeToConvert, context);
    }

    public void Write(BinaryStructWriter writer, long offset, object? value, BinaryStructSerializerContext context)
    {
        WriteValue(writer, offset, (T?)value, context);
    }
}
