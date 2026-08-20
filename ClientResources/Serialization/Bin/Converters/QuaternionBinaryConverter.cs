using System.Numerics;

namespace AllodsOnlineEditorTools.ClientResources.Serialization.Bin.Converters;

internal class QuaternionBinaryConverter : BinaryConverter<Quaternion>
{
    public override int GetSize(Type type, BinaryStructSerializerContext context) => 16;

    protected override Quaternion ReadValue(ref BinaryStructReader reader, long offset, Type typeToConvert, BinaryStructSerializerContext context)
    {
        var x = reader.ReadFloat(offset);
        var y = reader.ReadFloat(offset + 4);
        var z = reader.ReadFloat(offset + 8);
        var w = reader.ReadFloat(offset + 12);
        return new Quaternion(x, y, z, w);
    }

    protected override void WriteValue(BinaryStructWriter writer, long offset, Quaternion value, BinaryStructSerializerContext context)
    {
        throw new NotImplementedException();
    }
}
