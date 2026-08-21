using AllodsOnlineEditorTools.ClientResources.DataTypes;

namespace AllodsOnlineEditorTools.ClientResources.Serialization.Bin.Converters;

internal class FileRefBinaryConverter : BinaryConverter<FileRef>
{
    public override int GetSize(Type type, BinaryStructSerializerContext context) =>
        context.FileRefKind is FileRefKind.FileRef2 or FileRefKind.PakFileRef ? 20 : 12;

    protected override FileRef ReadValue(ref BinaryStructReader reader, long offset, Type typeToConvert, BinaryStructSerializerContext context)
    {
        if (context.FileRefKind == FileRefKind.None)
        {
            throw new NotSupportedException("Cannot read FileRef for version with unspecified FileRefKind");
        }

        string file;

        if (context.FileRefKind == FileRefKind.PakFileRef)
        {
            var metadata = context.CurrentDatabaseMetadata;
            var packIndex = reader.ReadInt(offset + 12);
            var fileIndex = reader.ReadInt(offset + 16);
            file = context.ResolvePakFileRef(packIndex, fileIndex);

            if (file.Length > 0 && metadata.PakFileRefOffsets is not null && !metadata.PakFileRefOffsets.Contains((int)offset))
            {
                throw new InvalidDataException($"PakFileRef at offset {offset} pointing to {file} is not listed in the database's PakFileRef offset table");
            }
        }
        else
        {
            file = reader.ReadString(offset);
        }

        FieldValidator.ValidateFileRef(file, offset);
        return new FileRef(file);
    }

    protected override void WriteValue(BinaryStructWriter writer, long offset, FileRef value, BinaryStructSerializerContext context)
    {
        throw new NotImplementedException();
    }
}
