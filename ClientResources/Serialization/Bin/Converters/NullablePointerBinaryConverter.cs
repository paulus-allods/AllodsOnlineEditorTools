using System.Diagnostics;
using AllodsOnlineEditorTools.ClientResources.DataTypes;
using AllodsOnlineEditorTools.ClientResources.Serialization.Bin.Database;

namespace AllodsOnlineEditorTools.ClientResources.Serialization.Bin.Converters;

internal class NullablePointerBinaryConverter : BinaryConverter<NullablePointer>
{
    public override int GetSize(Type type, BinaryStructSerializerContext context) => 4;

    protected override NullablePointer ReadValue(ref BinaryStructReader reader, long offset, Type typeToConvert,
        BinaryStructSerializerContext context)
    {
        if (!reader.TryGetPointerFix(offset, out var pointerFix))
        {
            // Object is null
            return NullablePointer.Empty;
        }

        Debug.Assert(pointerFix.Type == PointerFix.FixType.DbIdRef);
        var type = reader.ReadType(pointerFix.Value, true);
        var nested = reader.ReadObject(pointerFix.Value, type);
        Debug.Assert(nested != null);
        return new NullablePointer(nested);
    }

    protected override void WriteValue(BinaryStructWriter writer, long offset, NullablePointer value, BinaryStructSerializerContext context)
    {
        throw new NotImplementedException();
    }
}
