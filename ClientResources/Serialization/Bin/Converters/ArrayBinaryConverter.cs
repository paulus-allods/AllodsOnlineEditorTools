using System.Diagnostics;

namespace AllodsOnlineEditorTools.ClientResources.Serialization.Bin.Converters;

internal class ArrayBinaryConverter : BinaryConverter<Array>
{
    public override int GetSize(Type type, BinaryStructSerializerContext context) => 16;

    public override bool CanConvert(Type type)
    {
        return type.IsArray;
    }

    protected override Array ReadValue(ref BinaryStructReader reader, int offset, Type typeToConvert,
        BinaryStructSerializerContext context)
    {
        var elementType = typeToConvert.GetElementType();
        Debug.Assert(elementType is not null);
        if (!reader.TryGetPointerFix(offset, out var pointerFix))
        {
            // Array is empty
            return Array.CreateInstance(elementType, 0);
        }

        Debug.Assert(pointerFix.Type == PointerFix.FixType.Direct);
        var arrayMemorySize = reader.ReadInt(offset + 4);
        var elementSize = reader.GetSize(elementType);
        var elementCount = arrayMemorySize / elementSize;
        var result = Array.CreateInstance(elementType, elementCount);

        for (var i = 0; i < elementCount; i++)
        {
            var element = reader.ReadField(pointerFix.Value + i * elementSize, elementType);
            result.SetValue(element, i);
        }

        return result;
    }

    protected override void WriteValue(BinaryStructWriter writer, int offset, Array? value,
        BinaryStructSerializerContext context)
    {
        throw new NotImplementedException();
    }
}
