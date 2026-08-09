using System.Buffers.Binary;
using System.Diagnostics;
using System.Text;

namespace AllodsOnlineEditorTools.ClientResources.Serialization.Bin;

public ref struct BinaryStructReader(ReadOnlySpan<byte> buffer, BinaryStructSerializerContext context, BinarySerializerOptions options)
{
    private readonly ReadOnlySpan<byte> _buffer = buffer;

    public Type ReadType(int offset, bool nullable)
    {
        if (!context.CurrentDatabaseMetadata.Fixes.TryGetValue(offset, out var fix))
        {
            throw new InvalidDataException($"No pointer fix found at offset {offset}");
        }
        if (fix.Type is not (PointerFix.FixType.Type or PointerFix.FixType.Generic))
        {
            throw new InvalidDataException($"Expected a type pointer fix at offset {offset}, got {fix.Type}");
        }
        var structName = context.CurrentDatabaseMetadata.Structs[fix.Value];
        if (!context.TypeResolver.TryResolveByName(structName, out var type))
            throw new InvalidOperationException($"Struct implementation not found for '{structName}'");
        //BUG: Debug.Assert(ReadInt(offset + 16) == 1 || nullable || type.Name == "Territory");
        //BUG: TerritoriesRegistry + NameRules + Textures in V14 (all localized ?)
        Debug.Assert(ReadInt(offset + 4) == 1 || ReadInt(offset + 4) == 2 && type.Name == "Territory");
        return type;
    }

    public object ReadObject(int offset, Type type)
    {
        if (type.IsAbstract)
        {
            throw new InvalidOperationException($"Cannot read abstract type '{type.Name}'");
        }
        var result = Activator.CreateInstance(type)
            ?? throw new InvalidOperationException($"Failed to create instance of '{type.Name}'");
        foreach (var field in StructModelCache.Get(type).Fields)
        {
            if (field.Offset is not { } fieldOffset)
            {
                throw new InvalidOperationException($"Field '{type.Name}.{field.Name}' is missing {nameof(FieldOffsetAttribute)}");
            }
            var value = ReadField(offset + fieldOffset, field.FieldType);
            if (field.EnumRef is not null)
            {
                FieldValidator.ValidateEnumRef(field.Field, offset + fieldOffset, field.EnumRef, value);
            }
            field.SetValue(result, value);
        }
        return result;
    }
    
    public object? ReadField(int offset, Type type)
    {
        var converter = options.GetConverter(type);
        if (converter is not null)
        {
            return converter.Read(ref this, offset, type, context);
        }
        return type.IsClass ? ReadObject(offset, type) : throw new InvalidOperationException($"No binary converter registered for type '{type.Name}'");
    }


    public int ReadInt(int offset)
    {
        return BinaryPrimitives.ReadInt32LittleEndian(_buffer.Slice(offset, 4));
    }

    public long ReadLong(int offset)
    {
        return BinaryPrimitives.ReadInt64LittleEndian(_buffer.Slice(offset, 8));
    }

    public float ReadFloat(int offset)
    {
        return BinaryPrimitives.ReadSingleLittleEndian(_buffer.Slice(offset, 4));
    }

    public double ReadDouble(int offset)
    {
        return BinaryPrimitives.ReadDoubleLittleEndian(_buffer.Slice(offset, 8));
    }

    public bool ReadBool(int offset)
    {
        return _buffer[offset] != 0;
    }

    // Default string payloads (plain strings, file refs, text-file refs) are single-byte/ASCII;
    // only fields the schema marks as wide (WString) are UTF-16LE. The length prefix is a byte count either way.
    public string ReadString(int offset)
    {
        var result = ReadString(offset, Encoding.UTF8);
        Debug.Assert(!HasInvalidControlCharacters(result),
            $"String at offset {offset} contains invalid control characters (likely a WString/String encoding mismatch): '{result}'");
        return result;
    }

    public string ReadUnicodeString(int offset) => ReadString(offset, Encoding.Unicode);

    // Control characters other than tab/newline/carriage-return: their presence in a decoded string
    // usually signals a String/WString encoding mismatch (single-byte data read as UTF-16LE or vice versa).
    public static bool HasInvalidControlCharacters(string value) =>
        value.Any(c => c < 0x20 && c != '\t' && c != '\n' && c != '\r');

    private string ReadString(int offset, Encoding encoding)
    {
        if (!context.CurrentDatabaseMetadata.Fixes.TryGetValue(offset, out var fix))
        {
            return string.Empty;
        }
        if (fix.Type != PointerFix.FixType.Direct)
        {
            throw new InvalidDataException($"Expected a direct pointer fix for string at offset {offset}, got {fix.Type}");
        }
        var length = ReadInt(offset + 4);
        if (length < 0)
        {
            throw new InvalidDataException($"Negative string length ({length}) at offset {offset}");
        }
        return length > 0 ? encoding.GetString(_buffer.Slice(fix.Value, length)).TrimEnd('\0') : string.Empty;
    }

    public bool TryGetPointerFix(int offset, out PointerFix pointerFix)
    {
        return context.CurrentDatabaseMetadata.Fixes.TryGetValue(offset, out pointerFix);
    }
    
    public int GetSize(Type type) => options.GetTypeSize(type, context);
}