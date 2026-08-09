using System.Numerics;

namespace AllodsOnlineEditorTools.ClientResources.Serialization.Bin.Converters;

internal class Vector2BinaryConverter : BinaryConverter<Vector2>
{
    public override int GetSize(Type type, BinaryStructSerializerContext context) => 8;

    protected override Vector2 ReadValue(ref BinaryStructReader reader, int offset, Type typeToConvert, BinaryStructSerializerContext context)
    {
        var x = reader.ReadFloat(offset);
        var y = reader.ReadFloat(offset + 4);
        return new Vector2(x, y);
    }

    protected override void WriteValue(BinaryStructWriter writer, int offset, Vector2 value, BinaryStructSerializerContext context)
    {
        throw new NotImplementedException();
    }
}