using AllodsOnlineEditorTools.ClientResources.DataTypes;

namespace AllodsOnlineEditorTools.ClientResources.Serialization.Bin.Converters;

internal class BigVector3BinaryConverter : BinaryConverter<BigVector3>
{
    public override int GetSize(Type type, BinaryStructSerializerContext context) => 24;

    protected override BigVector3 ReadValue(ref BinaryStructReader reader, int offset, Type typeToConvert,
        BinaryStructSerializerContext context)
    {
        var localX = reader.ReadFloat(offset);
        var localY = reader.ReadFloat(offset + 4);
        var localZ = reader.ReadFloat(offset + 8);
        var globalX = reader.ReadInt(offset + 12);
        var globalY = reader.ReadInt(offset + 16);
        var globalZ = reader.ReadInt(offset + 20);
        return new BigVector3(globalX, globalY, globalZ, localX, localY, localZ);
    }

    protected override void WriteValue(BinaryStructWriter writer, int offset, BigVector3 value,
        BinaryStructSerializerContext context)
    {
        throw new NotImplementedException();
    }
}
