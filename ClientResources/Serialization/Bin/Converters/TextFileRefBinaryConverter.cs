using System.Diagnostics;
using AllodsOnlineEditorTools.ClientResources.DataTypes;

namespace AllodsOnlineEditorTools.ClientResources.Serialization.Bin.Converters;

internal class TextFileRefBinaryConverter : BinaryConverter<TextFileRef>
{
    public override int GetSize(Type type, BinaryStructSerializerContext context) => 16;

    protected override TextFileRef ReadValue(ref BinaryStructReader reader, int offset, Type typeToConvert, BinaryStructSerializerContext context)
    {
        var txtFile = reader.ReadString(offset);
        FieldValidator.ValidateTextFileRef(txtFile, offset);
        var id = reader.ReadInt(offset + 12);
        // I have only seen id = -1 in V7+ up to now
        Debug.Assert(id == -1 && txtFile == "" || (context.MainDatabaseMetadata.TextFileRefNames.TryGetValue(id, out var tableName) && tableName == txtFile),
            $"TextFileRef at offset {offset} has txt-files table id {id} which does not map back to '{txtFile}'");
        return new TextFileRef(txtFile);
    }

    protected override void WriteValue(BinaryStructWriter writer, int offset, TextFileRef value, BinaryStructSerializerContext context)
    {
        throw new NotImplementedException();
    }
}