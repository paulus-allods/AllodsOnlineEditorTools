using System.IO.Compression;

namespace AllodsOnlineEditorTools.ClientResources.Serialization;

public class PacksRegistry
{
    private readonly Dictionary<string, List<string>> _packsFiles;

    private PacksRegistry()
    {
        _packsFiles = [];
    }

    public static PacksRegistry Load(string packsDirectory)
    {
        var packsRegistry = new PacksRegistry();

        foreach (var file in Directory.GetFiles(packsDirectory, "*.pak"))
        {
            using var fs = File.Open(file, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var zip = new ZipArchive(fs, ZipArchiveMode.Read);
            var content = zip.Entries.Select(entry => entry.FullName).ToList();

            packsRegistry._packsFiles.Add(Path.GetFileName(file), content);
        }

        return packsRegistry;
    }

    public string GetFilename(string packName, int fileIndex)
    {
        return _packsFiles[packName][fileIndex];
    }
}
