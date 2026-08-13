using System.Diagnostics;
using AllodsOnlineEditorTools.ClientResources.DataTypes;

namespace AllodsOnlineEditorTools.ClientResources.Serialization.Bin.Converters;

internal class ResourcePointerBinaryConverter : BinaryConverter<ResourcePointer>
{
    public override int GetSize(Type type, BinaryStructSerializerContext context) => 8;

    protected override ResourcePointer ReadValue(ref BinaryStructReader reader, int offset, Type typeToConvert,
        BinaryStructSerializerContext context)
    {
        if (!reader.TryGetPointerFix(offset, out var pointerFix))
        {
            return ResourcePointer.Empty;
        }

        Debug.Assert(reader.ReadInt(offset) == 0);
        //BUG: Debug.Assert(reader.ReadInt(offset + 4) == 0);
        Debug.Assert(pointerFix.Type == PointerFix.FixType.DbIdRef);

        var database = pointerFix.External ? context.MainDatabaseMetadata : context.CurrentDatabaseMetadata;
        var file = database.Dbid2File[pointerFix.Value];

        var structName = database.GetStructType(pointerFix.Value)
                         ?? throw new InvalidOperationException($"No struct type for DbId {pointerFix.Value}.");
        var type = context.TypeResolver.TryResolveByName(structName, out var impl) ? impl : null;
        return new ResourcePointer(file, type);
    }

    protected override void WriteValue(BinaryStructWriter writer, int offset, ResourcePointer value,
        BinaryStructSerializerContext context)
    {
        throw new NotImplementedException();
    }
}
