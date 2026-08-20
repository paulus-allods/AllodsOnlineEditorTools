namespace AllodsOnlineEditorTools.ClientResources.Serialization.Bin;

public static class BinaryStructSerializer
{
    public static object Deserialize(ReadOnlySpan<byte> buffer, long offset, BinaryStructSerializerContext context,
        BinarySerializerOptions options)
    {
        var reader = new BinaryStructReader(buffer, context, options);
        var type = reader.ReadType(offset, false);
        return reader.ReadObject(offset, type);
    }

    public static byte[] Serialize(object obj, BinaryStructSerializerContext context, BinarySerializerOptions options)
    {
        var writer = new BinaryStructWriter(context, options);
        writer.WriteObject(0, obj, obj.GetType());
        return writer.ToArray();
    }
}
