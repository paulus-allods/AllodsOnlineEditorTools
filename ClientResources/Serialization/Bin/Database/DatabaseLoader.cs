using System.IO.Compression;
using AllodsOnlineEditorTools.ClientResources.Structs;
using Microsoft.Extensions.Logging;

namespace AllodsOnlineEditorTools.ClientResources.Serialization.Bin.Database;

public static class DatabaseLoader
{
    public static Dictionary<string, BinDatabase> LoadDatabases(string binPath, ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger(typeof(DatabaseLoader));
        var result = new Dictionary<string, BinDatabase>();

        if (File.Exists(binPath) && Path.GetExtension(binPath).Equals(".pak", StringComparison.OrdinalIgnoreCase))
        {
            logger.LogInformation("Loading packs from compressed pak {BinPath}, will use Bin folder inside ...", binPath);

            using var fs = File.OpenRead(binPath);
            using var zip = new ZipArchive(fs, ZipArchiveMode.Read);

            foreach (var entry in zip.Entries)
            {
                if (Path.GetDirectoryName(entry.FullName) == "Bin" && Path.GetExtension(entry.FullName) == ".bin")
                {
                    var fileName = Path.GetFileName(entry.FullName);
                    using var compressedBin = entry.Open();
                    var database = LoadDatabase(fileName, compressedBin, logger);
                    result[fileName] = database;
                }
            }
        }
        else if (Directory.Exists(binPath))
        {
            logger.LogInformation("Loading packs from folder {BinPath} ...", binPath);

            foreach (var file in Directory.GetFiles(binPath, "*.bin"))
            {
                var fileName = Path.GetFileName(file);
                using Stream compressedBin = File.OpenRead(file);
                var database = LoadDatabase(fileName, compressedBin, logger);
                result[fileName] = database;
            }
        }
        else
        {
            throw new ArgumentException("Unsupported Bin argument");
        }

        return result;
    }

    private static BinDatabase LoadDatabase(string name, Stream compressed, ILogger logger)
    {
        using var decompressed = new MemoryStream();
        using (var inflater = new ZLibStream(compressed, CompressionMode.Decompress))
        {
            inflater.CopyTo(decompressed);
        }

        var database = BinDatabaseReader.Read(decompressed, name, logger);

        var versionName = GameVersion.TryGetByVersion(database.Metadata.Version, out var version) ? version.ToString() : "unknown";
        var rootCount = database.Metadata.File2DbId.Count;
        var fileCount = database.Metadata.DbId2ObjId is { } dbId2ObjId ? $"{rootCount + dbId2ObjId.Count} ({rootCount} root)" : $"{rootCount}";
        logger.LogInformation("Loaded database: {File}, version {Version}, {Files} files, {Structs} structs", name, versionName, fileCount,
            database.Metadata.Structs.Count);

        return database;
    }
}
