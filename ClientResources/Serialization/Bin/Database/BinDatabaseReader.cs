using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.Logging;

namespace AllodsOnlineEditorTools.ClientResources.Serialization.Bin.Database;

/// <summary>
/// Reads the memory-image <c>.bin</c> database layouts.
/// </summary>
internal sealed class BinDatabaseReader
{
    private const int HeaderSize = 12;
    private const int HashTableMaxBucketCount = 65521;

    private readonly WordSize _wordSize;
    private readonly DatabaseFormat _databaseFormat;

    // The format and word size are discovered from the payload before construction, so an instance is
    // immutable and bound to a single database. Callers go through the static Read entry point below.
    private BinDatabaseReader(DatabaseFormat databaseFormat, WordSize wordSize)
    {
        _databaseFormat = databaseFormat;
        _wordSize = wordSize;
    }

    public static BinDatabase Read(Stream decompressed, string name, ILogger logger)
    {
        decompressed.Seek(0, SeekOrigin.Begin);
        using var reader = new BinaryReader(decompressed, Encoding.UTF8, leaveOpen: true);

        var (format, wordSize) = DetectDatabaseFormat(reader);
        return new BinDatabaseReader(format, wordSize).ReadDatabase(reader, name, logger);
    }

    private static (DatabaseFormat, WordSize) DetectDatabaseFormat(BinaryReader reader)
    {
        var oldPos = reader.BaseStream.Position;
        reader.BaseStream.Seek(0, SeekOrigin.Begin);
        var firstInt = reader.ReadInt32();

        // In V1 first int is header chunk Id
        var format = firstInt == (int)DatabaseChunkId.Header ? DatabaseFormat.V1 : DatabaseFormat.V2;
        if (format == DatabaseFormat.V1)
        {
            reader.BaseStream.Seek(oldPos, SeekOrigin.Begin);
            return (format, WordSize.X86);
        }

        reader.BaseStream.Seek(HeaderSize, SeekOrigin.Begin);
        var metadataId = reader.ReadInt32();
        var metadataSize = reader.ReadInt32();
        if (metadataId != (int)DatabaseChunkId.Metadata || metadataSize <= 0)
        {
            throw new InvalidDataException("Cannot determine word size, database has invalid format");
        }

        // In the 64-bit format the next 32 bits are equal to 0. In 32-bit, they are the offset of the first hash table
        var next = reader.ReadInt32();
        var wordSize = next == 0 ? WordSize.X64 : WordSize.X86;
        reader.BaseStream.Seek(oldPos, SeekOrigin.Begin);
        return (format, wordSize);
    }

    private BinDatabase ReadDatabase(BinaryReader reader, string name, ILogger logger)
    {
        var version = _databaseFormat == DatabaseFormat.V1 ? ReadChunk(reader, DatabaseChunkId.Header) : reader.ReadBytes(HeaderSize);
        var textFileRefNames = _databaseFormat == DatabaseFormat.V1 ? ReadTextFileRefNames(ReadChunk(reader, DatabaseChunkId.TxtFiles)) : null;
        var metadata = ReadMetadata(ReadChunk(reader, DatabaseChunkId.Metadata), name, logger);
        var data = ReadChunk(reader, DatabaseChunkId.Data);
        var fixes = ReadFixes(ReadChunk(reader, DatabaseChunkId.Fixes, _wordSize.FixEntrySize));

        HashSet<int>? pakFileRefOffsets = null;
        List<string>? packs = null;

        // The optional pak-file-ref / packs section is absent from legacy databases that reference no paks;
        // V2 always emits it. Either way it runs to end-of-stream, so the same EOF guard covers both.
        if (reader.BaseStream.Position != reader.BaseStream.Length)
        {
            pakFileRefOffsets = ReadPakFileRefOffsets(
                ReadChunk(reader, DatabaseChunkId.PakFileRefs, _wordSize.PointerSize), data.Length);

            var packsChunkId = (DatabaseChunkId)reader.ReadInt32();
            if (packsChunkId != DatabaseChunkId.Packs)
            {
                throw new InvalidDataException($"Expected chunk id {DatabaseChunkId.Packs}, got {packsChunkId}");
            }

            packs = ReadPacks(reader);
        }

        return new BinDatabase
        {
            Metadata = new DatabaseMetadata
            {
                TextFileRefNames = textFileRefNames,
                ObjId2DbId = metadata.ObjId2DbId,
                DbId2ObjId = metadata.DbId2ObjId,
                DbId2File = metadata.DbId2File,
                File2DbId = metadata.File2DbId,
                ResId2DbId = metadata.ResId2DbId,
                DbId2ResId = metadata.DbId2ResId,
                Fixes = fixes,
                Structs = metadata.Structs,
                Version = version,
                ResourceSystemVersion = metadata.ResourceSystemVersion,
                PakFileRefOffsets = pakFileRefOffsets,
                Packs = packs,
            },
            Data = data,
        };
    }

    private byte[] ReadChunk(BinaryReader reader, DatabaseChunkId expectedId, int entrySize = 1)
    {
        var chunkId = (DatabaseChunkId)reader.ReadInt32();
        if (chunkId != expectedId)
        {
            throw new InvalidDataException($"Expected chunk id {expectedId}, got {chunkId}");
        }

        // The size field is pointer-sized (s32 on x86, s64 on x64); it counts bytes for the
        // metadata/data chunks and entries for the fixes/pak-ref chunks.
        var count = _wordSize.ReadWord(reader);
        return reader.ReadBytes(checked((int)(count * entrySize)));
    }

    private static byte[] ReadBlockAt(BinaryReader reader, long offset, int size)
    {
        var back = reader.BaseStream.Position;
        reader.BaseStream.Seek(offset, SeekOrigin.Begin);
        var block = reader.ReadBytes(size);
        reader.BaseStream.Seek(back, SeekOrigin.Begin);
        return block;
    }

    // A directory / bucket entry: (s32 rel, s32 count). rel is relative to its own position and stays s32
    // in both builds; the returned offset is absolute within the metadata chunk.
    private static (long Offset, int Count) ReadDirectoryEntry(BinaryReader reader)
    {
        var offset = reader.BaseStream.Position + reader.ReadInt32();
        var count = reader.ReadInt32();
        return (offset, count);
    }

    // Walks a bucketed hash table: seek to the table, then for each of the bucketCount buckets read its
    // (rel, count) directory entry, seek to the bucket's records, invoke readRecord once per record (with
    // the bucket index, for hash validation), and restore the reader to the next directory entry. Each
    // readRecord call must leave the reader positioned at the start of the following record.
    private static void ReadHashTable(BinaryReader reader, long tableOffset, int bucketCount, Action<int> readRecord)
    {
        reader.BaseStream.Seek(tableOffset, SeekOrigin.Begin);

        for (var bucket = 0; bucket < bucketCount; bucket++)
        {
            var (recordsOffset, recordCount) = ReadDirectoryEntry(reader);
            var bucketBack = reader.BaseStream.Position;
            reader.BaseStream.Seek(recordsOffset, SeekOrigin.Begin);

            for (var record = 0; record < recordCount; record++)
            {
                readRecord(bucket);
            }

            reader.BaseStream.Seek(bucketBack, SeekOrigin.Begin);
        }
    }

    private static IDictionary<int, string> ReadTextFileRefNames(byte[] txtFilesChunk)
    {
        var textFileRefNames = new Dictionary<int, string>();

        using var stream = new MemoryStream(txtFilesChunk);
        using var reader = new BinaryReader(stream);
        var offset = reader.ReadInt32();
        var size = reader.ReadInt32();
        reader.BaseStream.Seek(offset, SeekOrigin.Begin);

        for (var i = 0; i < size; i++)
        {
            var dataOffset = reader.BaseStream.Position + reader.ReadInt32();
            var dataSize = reader.ReadInt32();
            var id = reader.ReadInt32();
            var rawData = ReadBlockAt(reader, dataOffset, dataSize);
            var txtFile = Encoding.UTF8.GetString(rawData).TrimEnd('\0');
            textFileRefNames.Add(id, txtFile);
        }

        return textFileRefNames;
    }

    private record MetadataChunk(
        int ResourceSystemVersion,
        IDictionary<long, string> DbId2File,
        IDictionary<string, long> File2DbId,
        IDictionary<long, int> DbId2ResId,
        IDictionary<int, long> ResId2DbId,
        List<string> Structs,
        IDictionary<int, long>? ObjId2DbId,
        IDictionary<long, int>? DbId2ObjId);

    private MetadataChunk ReadMetadata(byte[] metadataChunk, string name, ILogger logger)
    {
        using var stream = new MemoryStream(metadataChunk);
        using var reader = new BinaryReader(stream);

        var (objId2DbIdOffset, objId2DbIdBucketCount) = _databaseFormat == DatabaseFormat.V2 ? ReadDirectoryEntry(reader) : (0L, 0);
        var (dbId2FileOffset, dbId2FileBucketCount) = ReadDirectoryEntry(reader);
        var (structsOffset, structsCount) = ReadDirectoryEntry(reader);
        var (resId2DbIdOffset, resId2DbIdCount) = ReadDirectoryEntry(reader);
        var (dbId2ResIdOffset, dbId2ResIdCount) = ReadDirectoryEntry(reader);
        if (_databaseFormat == DatabaseFormat.V2)
        {
            _wordSize.ReadWord(reader); // Data pointer (redundant with the Data chunk)
        }

        var resourceSystemVersion = (int)_wordSize.ReadWord(reader);

        Debug.Assert(resId2DbIdCount is HashTableMaxBucketCount or 0);
        Debug.Assert(dbId2ResIdCount is HashTableMaxBucketCount or 0);
        Debug.Assert(_databaseFormat == DatabaseFormat.V1 ? dbId2FileBucketCount is HashTableMaxBucketCount or 0 : objId2DbIdBucketCount is HashTableMaxBucketCount or 0);

        var (objId2DbId, dbId2ObjId) = _databaseFormat == DatabaseFormat.V2 ? ReadObjId2DbId(reader, objId2DbIdOffset, objId2DbIdBucketCount) : (null, null);
        var (dbId2File, file2DbId) = ReadDbId2File(reader, dbId2FileOffset, dbId2FileBucketCount);
        var structs = ReadStructs(reader, structsOffset, structsCount);
        var resId2DbId = ReadResId2DbId(reader, resId2DbIdOffset, resId2DbIdCount, dbId2File, name, logger);
        var dbId2ResId = ReadDbId2ResId(reader, dbId2ResIdOffset, dbId2ResIdCount);

        return new MetadataChunk(resourceSystemVersion, dbId2File, file2DbId, dbId2ResId, resId2DbId, structs, objId2DbId, dbId2ObjId);
    }

    private (IDictionary<long, string> DbId2File, IDictionary<string, long> File2DbId) ReadDbId2File(BinaryReader reader, long tableOffset, int bucketCount)
    {
        var dbId2File = new SortedDictionary<long, string>();
        var file2DbId = new SortedDictionary<string, long>();

        ReadHashTable(reader, tableOffset, bucketCount, bucket =>
        {
            var blockOffset = reader.BaseStream.Position + reader.ReadInt32();
            var blockSize = reader.ReadInt32();
            var dbId = _wordSize.ReadWord(reader);
            var recordBack = reader.BaseStream.Position;

            reader.BaseStream.Seek(blockOffset, SeekOrigin.Begin);
            var delimiter = reader.ReadInt32();
            if (delimiter != 1)
            {
                throw new InvalidDataException($"Expected dbid2file entry delimiter 1, got {delimiter}");
            }

            var adler32 = reader.ReadUInt32();
            if (adler32 % bucketCount != bucket)
            {
                throw new InvalidDataException(
                    $"dbId2file entry hash {adler32} does not match hash table bucket {bucket}");
            }

            // The block is [delimiter(4), adler32(4), name(size - 9), '\0']; the name is size - 9 bytes.
            var nameBytes = reader.ReadBytes(blockSize - 9);
            var computedChecksum = Adler32.Compute(nameBytes);
            if (computedChecksum != adler32)
            {
                throw new InvalidDataException(
                    $"dbId2file entry checksum mismatch: expected {adler32}, computed {computedChecksum}");
            }

            var filename = Encoding.UTF8.GetString(nameBytes).TrimEnd('\0');
            dbId2File.TryAdd(dbId, filename);
            file2DbId.TryAdd(filename, dbId);
            reader.BaseStream.Seek(recordBack, SeekOrigin.Begin);
        });

        return (dbId2File, file2DbId);
    }

    private (IDictionary<int, long>, IDictionary<long, int>) ReadObjId2DbId(BinaryReader reader, long tableOffset, int bucketCount)
    {
        var objId2DbId = new SortedDictionary<int, long>();
        var dbId2ObjId = new SortedDictionary<long, int>();

        ReadHashTable(reader, tableOffset, bucketCount, bucket =>
        {
            var blockOffset = reader.BaseStream.Position + reader.ReadInt32();
            var blockSize = reader.ReadInt32();
            var dbId = _wordSize.ReadWord(reader);
            var recordBack = reader.BaseStream.Position;

            reader.BaseStream.Seek(blockOffset, SeekOrigin.Begin);
            var delimiter = reader.ReadInt32();
            if (delimiter != 1)
            {
                throw new InvalidDataException($"Expected objId2dbid entry delimiter 1, got {delimiter}");
            }

            // The block is [delimiter(4), objId(4), '\0']: 9 bytes, no name.
            Debug.Assert(blockSize == 9);
            var objId = (int)reader.ReadUInt32();
            if (objId % bucketCount != bucket)
            {
                throw new InvalidDataException($"objId2dbid entry objId {objId} does not match hash table bucket {bucket}");
            }

            // objid <-> dbid is a bijection, so Add (not TryAdd): a duplicate signals a corrupt table.
            objId2DbId.Add(objId, dbId);
            dbId2ObjId.Add(dbId, objId);
            reader.BaseStream.Seek(recordBack, SeekOrigin.Begin);
        });

        return (objId2DbId, dbId2ObjId);
    }

    private List<string> ReadStructs(BinaryReader reader, long structsOffset, int structsCount)
    {
        // Entries are (s32 rel, s32 size, s32 delimiter) padded to pointer alignment (a trailing pad word
        // on x64, none on x86); the name is `size` bytes at rel.
        var structs = new List<string>(structsCount);
        reader.BaseStream.Seek(structsOffset, SeekOrigin.Begin);

        for (var i = 0; i < structsCount; i++)
        {
            var dataOffset = reader.BaseStream.Position + reader.ReadInt32();
            var dataSize = reader.ReadInt32();
            var delimiter = reader.ReadInt32();
            if (delimiter != 0)
            {
                throw new InvalidDataException($"Expected structs entry delimiter 0, got {delimiter}");
            }

            if (_wordSize.PointerSize == 8)
            {
                reader.ReadInt32(); // alignment padding
            }

            var rawData = ReadBlockAt(reader, dataOffset, dataSize);
            var structName = Encoding.UTF8.GetString(rawData).TrimEnd('\0').Replace("struct NDb::", "");
            structs.Add(structName);
        }

        return structs;
    }

    private IDictionary<int, long> ReadResId2DbId(BinaryReader reader, long tableOffset, int bucketCount,
        IDictionary<long, string> dbId2File, string name, ILogger logger)
    {
        // 65521 buckets of (s32 rel, s32 count); each pair is (pointer resId, pointer dbId), bucketed by
        // resId % 65521.
        var map = new SortedDictionary<int, long>();

        ReadHashTable(reader, tableOffset, bucketCount, bucket =>
        {
            var resId = (int)_wordSize.ReadWord(reader);
            var dbId = _wordSize.ReadWord(reader);
            Debug.Assert(resId % HashTableMaxBucketCount == bucket);

            if (!map.TryAdd(resId, dbId))
            {
                logger.LogWarning("In {Database}, files {ExistingFile} and {File} have the same resource id {ResId}",
                    name, FileName(dbId2File, map[resId]), FileName(dbId2File, dbId), resId);
            }
        });

        return map;
    }

    private IDictionary<long, int> ReadDbId2ResId(BinaryReader reader, long tableOffset, int bucketCount)
    {
        // 65521 buckets of (s32 rel, s32 count); each pair is (pointer dbId, pointer resId), bucketed by
        // dbId % 65521.
        var map = new SortedDictionary<long, int>();

        ReadHashTable(reader, tableOffset, bucketCount, bucket =>
        {
            var dbId = _wordSize.ReadWord(reader);
            var resId = (int)_wordSize.ReadWord(reader);
            Debug.Assert(dbId % HashTableMaxBucketCount == bucket);
            map.TryAdd(dbId, resId);
        });

        return map;
    }

    private static string FileName(IDictionary<long, string> dbId2File, long dbId) => dbId2File.TryGetValue(dbId, out var file) ? file : $"dbId {dbId}";

    private IDictionary<long, PointerFix> ReadFixes(byte[] fixesChunk)
    {
        // Entries are (pointer data, pointer value); the address is packed in the high bits of data and
        // scales by the pointer size (x4 on x86, x8 on x64).
        using var stream = new MemoryStream(fixesChunk);
        using var reader = new BinaryReader(stream);

        var fixes = new SortedDictionary<long, PointerFix>();
        var entryCount = fixesChunk.Length / _wordSize.FixEntrySize;

        for (var i = 0; i < entryCount; i++)
        {
            var data = _wordSize.ReadWord(reader);
            var value = _wordSize.ReadWord(reader);
            var address = (data >> 3) * _wordSize.FixAddressScale;
            var type = (PointerFix.FixType)(data & 3);
            if (!Enum.IsDefined(type))
            {
                throw new InvalidDataException($"Unknown pointer fix type {data & 3} at fix entry {i}");
            }

            fixes.Add(address, new PointerFix(type, (data & 4) > 0, value));
        }

        return fixes;
    }

    private HashSet<int> ReadPakFileRefOffsets(byte[] pakFileRefsChunk, int dataLength)
    {
        using var stream = new MemoryStream(pakFileRefsChunk);
        using var reader = new BinaryReader(stream);

        HashSet<int> offsets = [];
        var entryCount = pakFileRefsChunk.Length / _wordSize.PointerSize;

        for (var i = 0; i < entryCount; i++)
        {
            var offset = (int)(_wordSize.ReadWord(reader) * _wordSize.PointerSize - _wordSize.PakFileRefSubtrahend);
            if (offset < 0 || offset >= dataLength)
            {
                throw new InvalidDataException($"PakFileRef offset {offset} at entry {i} is outside the data chunk (size {dataLength})");
            }

            offsets.Add(offset);
        }

        return offsets;
    }

    private List<string> ReadPacks(BinaryReader reader)
    {
        // The Packs chunk carries a count rather than a byte size; the count and per-name length are
        // pointer-sized and each name is UTF-16.
        var packsAmount = _wordSize.ReadWord(reader);

        var packs = new List<string>((int)packsAmount);
        for (long i = 0; i < packsAmount; i++)
        {
            var size = (int)_wordSize.ReadWord(reader);
            var rawData = reader.ReadBytes(size);
            var pack = Encoding.Unicode.GetString(rawData).TrimEnd('\0');
            packs.Add(Path.GetFileName(pack));
        }

        return packs;
    }
}
