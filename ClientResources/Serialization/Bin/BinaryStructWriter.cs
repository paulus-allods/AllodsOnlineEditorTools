using System.Buffers.Binary;

namespace AllodsOnlineEditorTools.ClientResources.Serialization.Bin;

public sealed class BinaryStructWriter(BinaryStructSerializerContext context, BinarySerializerOptions options)
{
    private byte[] _buffer = [];
    private int _length;

    private readonly SortedDictionary<int, PointerFix> _fixes = new();

    /// <summary>Pointer fixups accumulated while writing, keyed by the offset of the pointer they patch.</summary>
    public IReadOnlyDictionary<int, PointerFix> Fixes => _fixes;

    public void RegisterFix(int offset, PointerFix fix) => _fixes[offset] = fix;

    public void WriteObject(int offset, object value, Type type)
    {
        foreach (var field in StructModelCache.Get(type).Fields)
        {
            if (field.Offset is not { } fieldOffset)
            {
                throw new InvalidOperationException($"Field '{type.Name}.{field.Name}' is missing {nameof(FieldOffsetAttribute)}");
            }
            WriteField(offset + fieldOffset, field.GetValue(value), field.FieldType);
        }
    }

    public void WriteField(int offset, object? value, Type type)
    {
        var converter = options.GetConverter(type);
        if (converter is not null)
        {
            converter.Write(this, offset, value, context);
            return;
        }
        if (type.IsClass)
        {
            WriteObject(offset, value ?? throw new InvalidOperationException($"Cannot write null object of type '{type.Name}'"), type);
            return;
        }
        throw new InvalidOperationException($"No binary converter registered for type '{type.Name}'");
    }

    public void WriteInt(int offset, int value) => BinaryPrimitives.WriteInt32LittleEndian(Reserve(offset, 4), value);
    public void WriteLong(int offset, long value) => BinaryPrimitives.WriteInt64LittleEndian(Reserve(offset, 8), value);
    public void WriteFloat(int offset, float value) => BinaryPrimitives.WriteSingleLittleEndian(Reserve(offset, 4), value);
    public void WriteDouble(int offset, double value) => BinaryPrimitives.WriteDoubleLittleEndian(Reserve(offset, 8), value);
    public void WriteBool(int offset, bool value) => Reserve(offset, 1)[0] = (byte)(value ? 1 : 0);

    /// <summary>The bytes written so far, trimmed to the highest offset touched.</summary>
    public byte[] ToArray() => _buffer[.._length];

    private Span<byte> Reserve(int offset, int size)
    {
        var end = offset + size;
        if (end > _buffer.Length)
        {
            Array.Resize(ref _buffer, Math.Max(end, Math.Max(_buffer.Length * 2, 16)));
        }
        if (end > _length) _length = end;
        return _buffer.AsSpan(offset, size);
    }
}
