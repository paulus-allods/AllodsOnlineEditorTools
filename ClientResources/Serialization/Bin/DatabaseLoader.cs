using System.Buffers.Binary;
using System.Diagnostics;
using System.IO.Compression;
using System.Text;
using AllodsOnlineEditorTools.ClientResources.Structs;
using Microsoft.Extensions.Logging;

namespace AllodsOnlineEditorTools.ClientResources.Serialization.Bin;

public static class DatabaseLoader
{
    public record Result(Dictionary<string, DatabaseMetadata> Metadata, Dictionary<string, byte[]> Data);

    private const int HeaderChunkId = 0;
    private const int TxtFilesChunkId = 1;
    private const int MetadataChunkId = 2;
    private const int DataChunkId = 3;
    private const int FixesChunkId = 4;
    private const int PakFileRefsChunkId = 5;
    private const int PacksChunkId = 6;

    private const int PakFileRefPackIndexOffset = 12;

    public static Result LoadDatabases(string binPath, ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger(typeof(DatabaseLoader));
        var metadata = new Dictionary<string, DatabaseMetadata>();
        var data = new Dictionary<string, byte[]>();

        if (Path.GetExtension(binPath).Equals(".pak", StringComparison.OrdinalIgnoreCase))
        {
            logger.LogInformation("Loading packs from compressed pak {BinPath}, will use Bin folder inside ...",
                binPath);

            using var fs = File.OpenRead(binPath);
            using var zip = new ZipArchive(fs, ZipArchiveMode.Read);

            foreach (var entry in zip.Entries)
            {
                if (Path.GetDirectoryName(entry.FullName) == "Bin" && Path.GetExtension(entry.FullName) == ".bin")
                {
                    var fileName = Path.GetFileName(entry.FullName);
                    using var compressedBin = entry.Open();
                    LoadDatabase(fileName, compressedBin, metadata, data, logger);
                }
            }
        }
        else if (!Path.HasExtension(binPath))
        {
            logger.LogInformation("Loading packs from folder {BinPath} ...", binPath);

            foreach (var file in Directory.GetFiles(binPath, "*.bin"))
            {
                var fileName = Path.GetFileName(file);
                using Stream fs = File.OpenRead(file);
                LoadDatabase(fileName, fs, metadata, data, logger);
            }
        }
        else
        {
            throw new ArgumentException("Unsupported Bin argument");
        }

        return new Result(metadata, data);
    }

    private static void LoadDatabase(string name, Stream database, Dictionary<string, DatabaseMetadata> metadata,
        Dictionary<string, byte[]> data, ILogger logger)
    {
        var (databaseMetadata, databaseData) = Load(database, name, logger);
        metadata[name] = databaseMetadata;
        data[name] = databaseData;
        var versionName = GameVersion.Versions.TryGetValue(databaseMetadata.Version, out var version)
            ? version.ToString()
            : "unknown";
        logger.LogInformation("Loaded database: {File}, version {Version}, {Files} files, {Structs} structs", name,
            versionName, databaseMetadata.File2Dbid.Count, databaseMetadata.Structs.Count);
    }

    private static (DatabaseMetadata Metadata, byte[] Data) Load(Stream binStream, string name, ILogger logger)
    {
        using var memoryStream = new MemoryStream();
        using var inflaterInputStream = new ZLibStream(binStream, CompressionMode.Decompress);
        inflaterInputStream.CopyTo(memoryStream);

        memoryStream.Seek(0, SeekOrigin.Begin);

        using var reader = new BinaryReader(memoryStream);

        var headerChunk = ReadChunk(reader, HeaderChunkId);
        var version = BinaryPrimitives.ReadUInt64BigEndian(headerChunk);

        var textFileRefNames = ReadTextFileRefNames(ReadChunk(reader, TxtFilesChunkId));
        var metadata = ReadMetadata(ReadChunk(reader, MetadataChunkId), name, logger);
        var data = ReadChunk(reader, DataChunkId);
        var fixes = ReadFixes(ReadChunk(reader, FixesChunkId, entrySize: 8));

        HashSet<int>? pakFileRefOffsets = null;
        List<string>? packs = null;

        if (reader.BaseStream.Position != reader.BaseStream.Length)
        {
            pakFileRefOffsets = ReadPakFileRefOffsets(ReadChunk(reader, PakFileRefsChunkId, entrySize: 4), data.Length);

            var packsChunkId = reader.ReadInt32();
            if (packsChunkId != PacksChunkId)
            {
                throw new InvalidDataException($"Expected chunk id {PacksChunkId}, got {packsChunkId}");
            }

            var remaining = (int)(reader.BaseStream.Length - reader.BaseStream.Position);
            packs = ReadPacks(reader.ReadBytes(remaining));
        }

        return (new DatabaseMetadata
        {
            TextFileRefNames = textFileRefNames,
            Dbid2File = metadata.DbId2File,
            File2Dbid = metadata.File2DbId,
            Resid2Dbid = metadata.ResId2DbId,
            Dbid2Resid = metadata.DbId2ResId,
            Fixes = fixes,
            Structs = metadata.Structs,
            Version = version,
            ResourceSystemVersion = metadata.ResourceSystemVersion,
            PakFileRefOffsets = pakFileRefOffsets,
            Packs = packs,
        }, data);
    }

    // Adler32 checksum (RFC 1950): two 16-bit sums accumulated modulo the largest prime below 2^16.
    private static uint ComputeAdler32(ReadOnlySpan<byte> data)
    {
        const uint modAdler = 65521;
        uint a = 1, b = 0;
        foreach (var value in data)
        {
            a = (a + value) % modAdler;
            b = (b + a) % modAdler;
        }

        return (b << 16) | a;
    }

    private static byte[] ReadBlockAt(BinaryReader reader, long offset, int size)
    {
        var back = reader.BaseStream.Position;
        reader.BaseStream.Seek(offset, SeekOrigin.Begin);
        var block = reader.ReadBytes(size);
        reader.BaseStream.Seek(back, SeekOrigin.Begin);
        return block;
    }

    private static byte[] ReadChunk(BinaryReader reader, int expectedId, int entrySize = 1)
    {
        var chunkId = reader.ReadInt32();
        if (chunkId != expectedId)
        {
            throw new InvalidDataException($"Expected chunk id {expectedId}, got {chunkId}");
        }

        var chunkSize = reader.ReadInt32();
        return reader.ReadBytes(chunkSize * entrySize);
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
        IDictionary<int, string> DbId2File,
        IDictionary<string, int> File2DbId,
        IDictionary<int, int> DbId2ResId,
        IDictionary<int, int> ResId2DbId,
        List<string> Structs);

    private static MetadataChunk ReadMetadata(byte[] metadataChunk, string name, ILogger logger)
    {
        using var stream = new MemoryStream(metadataChunk);
        using var reader = new BinaryReader(stream);

        var dbId2FileOffset = reader.BaseStream.Position + reader.ReadInt32();
        Debug.Assert(dbId2FileOffset == 36);
        var dbId2FileSize = reader.ReadInt32();
        Debug.Assert(dbId2FileSize is 65521 or 0);

        var structsOffset = reader.BaseStream.Position + reader.ReadInt32();
        var structsSize = reader.ReadInt32();

        var resId2DbIdOffset = reader.BaseStream.Position + reader.ReadInt32();
        var resId2DbIdSize = reader.ReadInt32();
        Debug.Assert(resId2DbIdSize is 65521 or 0);

        var dbId2ResIdOffset = reader.BaseStream.Position + reader.ReadInt32();
        var dbId2ResIdSize = reader.ReadInt32();
        Debug.Assert(dbId2ResIdSize is 65521 or 0);

        var resourceSystemVersion = reader.ReadInt32();

        // DbId2file
        reader.BaseStream.Seek(dbId2FileOffset, SeekOrigin.Begin);

        var dbId2File = new SortedDictionary<int, string>();
        var file2DbId = new SortedDictionary<string, int>();

        for (var i = 0; i < dbId2FileSize; i++)
        {
            var entryOffset = reader.BaseStream.Position + reader.ReadInt32();
            var entrySize = reader.ReadInt32();
            var entryBack = reader.BaseStream.Position;

            reader.BaseStream.Seek(entryOffset, SeekOrigin.Begin);

            for (var j = 0; j < entrySize; j++)
            {
                var dataOffset = reader.BaseStream.Position + reader.ReadInt32();
                var dataSize = reader.ReadInt32();
                var dbId = reader.ReadInt32();
                var back = reader.BaseStream.Position;
                reader.BaseStream.Seek(dataOffset, SeekOrigin.Begin);
                var delimiter = reader.ReadInt32();
                if (delimiter != 1)
                {
                    throw new InvalidDataException($"Expected dbid2file entry delimiter 1, got {delimiter}");
                }

                var adler32 = reader.ReadUInt32();
                if (adler32 % 65521 != i)
                {
                    throw new InvalidDataException(
                        $"dbId2file entry hash {adler32} does not match hash table bucket {i}");
                }

                var rawData = reader.ReadBytes(dataSize);
                Debug.Assert(rawData.Length - Array.IndexOf(rawData, (byte)0) == 9);
                rawData = rawData[..Array.IndexOf(rawData, (byte)0)];
                var computedChecksum = ComputeAdler32(rawData);
                if (computedChecksum != adler32)
                {
                    throw new InvalidDataException(
                        $"dbId2file entry checksum mismatch: expected {adler32}, computed {computedChecksum}");
                }

                var filename = Encoding.UTF8.GetString(rawData).TrimEnd('\0');
                dbId2File.TryAdd(dbId, filename);
                file2DbId.TryAdd(filename, dbId);
                reader.BaseStream.Seek(back, SeekOrigin.Begin);
            }

            reader.BaseStream.Seek(entryBack, SeekOrigin.Begin);
        }


        // Structs

        reader.BaseStream.Seek(structsOffset, SeekOrigin.Begin);

        List<string> structs = [];

        for (var i = 0; i < structsSize; i++)
        {
            var dataOffset = reader.BaseStream.Position + reader.ReadInt32();
            var dataSize = reader.ReadInt32();
            var delimiter = reader.ReadInt32();
            if (delimiter != 0)
            {
                throw new InvalidDataException($"Expected structs entry delimiter 0, got {delimiter}");
            }

            var rawData = ReadBlockAt(reader, dataOffset, dataSize);
            var structName = Encoding.UTF8.GetString(rawData).TrimEnd('\0').Replace("struct NDb::", "");
            structs.Add(structName);
        }

        // ResId2DbId

        var resId2DbId = new SortedDictionary<int, int>();
        var dbId2ResId = new SortedDictionary<int, int>();

        reader.BaseStream.Seek(resId2DbIdOffset, SeekOrigin.Begin);

        for (var i = 0; i < resId2DbIdSize; i++)
        {
            var dataOffset = reader.BaseStream.Position + reader.ReadInt32();
            var dataSize = reader.ReadInt32();
            var back = reader.BaseStream.Position;

            reader.BaseStream.Seek(dataOffset, SeekOrigin.Begin);

            for (var j = 0; j < dataSize; j++)
            {
                var resid = reader.ReadInt32();
                Debug.Assert(resid % 65521 == i);
                var dbid = reader.ReadInt32();
                if (!resId2DbId.TryAdd(resid, dbid))
                {
                    var existingDbId = resId2DbId[resid];
                    var existingFile = dbId2File.GetValueOrDefault(existingDbId, $"dbId {existingDbId}");
                    var file = dbId2File.GetValueOrDefault(dbid, $"dbId {dbid}");
                    logger.LogWarning(
                        "In {Database}, files {ExistingFile} and {File} have the same resource id {ResId}",
                        name, existingFile, file, resid);
                }
            }

            reader.BaseStream.Seek(back, SeekOrigin.Begin);
        }

        // DbId2resId

        reader.BaseStream.Seek(dbId2ResIdOffset, SeekOrigin.Begin);

        for (var i = 0; i < dbId2ResIdSize; i++)
        {
            var dataOffset = reader.BaseStream.Position + reader.ReadInt32();
            var dataSize = reader.ReadInt32();
            var back = reader.BaseStream.Position;

            reader.BaseStream.Seek(dataOffset, SeekOrigin.Begin);

            for (var j = 0; j < dataSize; j++)
            {
                var dbId = reader.ReadInt32();
                Debug.Assert(dbId % 65521 == i);
                var resId = reader.ReadInt32();
                dbId2ResId.TryAdd(dbId, resId);
            }

            reader.BaseStream.Seek(back, SeekOrigin.Begin);
        }

        return new MetadataChunk(resourceSystemVersion, dbId2File, file2DbId, dbId2ResId, resId2DbId, structs);
    }

    private static IDictionary<int, PointerFix> ReadFixes(byte[] fixesChunk)
    {
        using var stream = new MemoryStream(fixesChunk);
        using var reader = new BinaryReader(stream);

        var fixes = new SortedDictionary<int, PointerFix>();

        for (var i = 0; i < fixesChunk.Length / 8; i++)
        {
            var data = reader.ReadInt32();
            var value = reader.ReadInt32();
            var address = (data >> 3) * 4;
            var type = (PointerFix.FixType)(data & 3);
            if (!Enum.IsDefined(type))
            {
                throw new InvalidDataException($"Unknown pointer fix type {data & 3} at fix entry {i}");
            }

            var fix = new PointerFix(type, (data & 4) > 0, value);
            fixes.Add(address, fix);
        }

        return fixes;
    }

    private static HashSet<int> ReadPakFileRefOffsets(byte[] pakFileRefsChunk, int dataLength)
    {
        using var stream = new MemoryStream(pakFileRefsChunk);
        using var reader = new BinaryReader(stream);

        HashSet<int> offsets = [];

        for (var i = 0; i < pakFileRefsChunk.Length / 4; i++)
        {
            var offset = reader.ReadInt32() * 4 - PakFileRefPackIndexOffset;
            if (offset < 0 || offset >= dataLength)
            {
                throw new InvalidDataException(
                    $"PakFileRef offset {offset} at entry {i} is outside the data chunk (size {dataLength})");
            }

            offsets.Add(offset);
        }

        return offsets;
    }

    private static List<string> ReadPacks(byte[] packsChunk)
    {
        using var stream = new MemoryStream(packsChunk);
        using var reader = new BinaryReader(stream);

        var packsAmount = reader.ReadInt32();

        List<string> packs = [];

        for (var i = 0; i < packsAmount; i++)
        {
            var size = reader.ReadInt32();
            var rawData = reader.ReadBytes(size);
            var pack = Encoding.Unicode.GetString(rawData, 0, rawData.Length).TrimEnd('\0');
            packs.Add(Path.GetFileName(pack));
        }

        return packs;
    }
}
