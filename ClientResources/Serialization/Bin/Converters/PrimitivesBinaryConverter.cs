namespace AllodsOnlineEditorTools.ClientResources.Serialization.Bin.Converters;

internal class PrimitivesBinaryConverter : BinaryConverter<object>
{
    public override int GetSize(Type type, BinaryStructSerializerContext context)
    {
        if (type == typeof(bool))
        {
            return 1;
        }

        if (type == typeof(int) || type == typeof(float))
        {
            return 4;
        }

        if (type == typeof(long) || type == typeof(double))
        {
            return 8;
        }

        if (type == typeof(string))
        {
            return 12;
        }

        throw new NotSupportedException($"Unknown primitive: {type.Name}");
    }

    public override bool CanConvert(Type type)
    {
        return type.IsPrimitive || type == typeof(string);
    }

    protected override object ReadValue(ref BinaryStructReader reader, long offset, Type typeToConvert, BinaryStructSerializerContext context)
    {
        if (typeToConvert == typeof(int))
        {
            return reader.ReadInt(offset);
        }

        if (typeToConvert == typeof(long))
        {
            return reader.ReadLong(offset);
        }

        if (typeToConvert == typeof(float))
        {
            return reader.ReadFloat(offset);
        }

        if (typeToConvert == typeof(double))
        {
            return reader.ReadDouble(offset);
        }

        if (typeToConvert == typeof(bool))
        {
            return reader.ReadBool(offset);
        }

        if (typeToConvert == typeof(string))
        {
            var value = reader.ReadString(offset);
            FieldValidator.ValidatePlainString(value, offset);
            return value;
        }

        throw new NotSupportedException($"Unknown primitive: {typeToConvert.Name}");
    }

    protected override void WriteValue(BinaryStructWriter writer, long offset, object? value, BinaryStructSerializerContext context)
    {
        throw new NotImplementedException();
    }
}
