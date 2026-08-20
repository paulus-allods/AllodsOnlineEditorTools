using System.Numerics;

namespace AllodsOnlineEditorTools.ClientResources.Serialization.Bin.Converters;

internal class Vector3BinaryConverter : BinaryConverter<Vector3>
{
    public override int GetSize(Type type, BinaryStructSerializerContext context) => 12;

    protected override Vector3 ReadValue(ref BinaryStructReader reader, long offset, Type typeToConvert, BinaryStructSerializerContext context)
    {
        var x = reader.ReadFloat(offset);
        var y = reader.ReadFloat(offset + 4);
        var z = reader.ReadFloat(offset + 8);
        return new Vector3(x, y, z);
    }

    protected override void WriteValue(BinaryStructWriter writer, long offset, Vector3 value, BinaryStructSerializerContext context)
    {
        throw new NotImplementedException();
    }
}
