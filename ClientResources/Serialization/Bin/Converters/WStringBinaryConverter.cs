using AllodsOnlineEditorTools.ClientResources.DataTypes;
using Microsoft.Extensions.Logging;

namespace AllodsOnlineEditorTools.ClientResources.Serialization.Bin.Converters;

internal class WStringBinaryConverter : BinaryConverter<WString>
{
    public override int GetSize(Type type, BinaryStructSerializerContext context) => 12;

    protected override WString ReadValue(ref BinaryStructReader reader, int offset, Type typeToConvert,
        BinaryStructSerializerContext context)
    {
        var value = reader.ReadUnicodeString(offset);
        if (BinaryStructReader.HasInvalidControlCharacters(value))
        {
            context.LoggerFactory?.CreateLogger<WStringBinaryConverter>().LogWarning(
                "WString at offset {Offset} contains invalid control characters (likely a WString/String encoding mismatch): '{Value}'; replacing with empty string",
                offset, value);
            value = string.Empty;
        }

        return new WString(value);
    }

    protected override void WriteValue(BinaryStructWriter writer, int offset, WString value,
        BinaryStructSerializerContext context)
    {
        throw new NotImplementedException();
    }
}
