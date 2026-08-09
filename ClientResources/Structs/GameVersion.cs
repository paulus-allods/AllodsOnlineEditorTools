using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using AllodsOnlineEditorTools.ClientResources.DataTypes;

namespace AllodsOnlineEditorTools.ClientResources.Structs;

public class GameVersion
{
    public static readonly IReadOnlyDictionary<ulong, GameVersion> Versions;
    public static readonly IReadOnlyDictionary<string, GameVersion> ByName;

    public string Hash { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public bool NeedPacks { get; set; }
    public string Game { get; set; } = string.Empty;
    public string Namespace { get; set; } = string.Empty;

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public FileRefKind FileRefKind { get; set; } = FileRefKind.None;

    public override string ToString() => $"{Game}: {Version} ({Hash})";

    static GameVersion()
    {
        var byHash = new Dictionary<ulong, GameVersion>();
        var byName = new Dictionary<string, GameVersion>(StringComparer.OrdinalIgnoreCase);
        var resourceManager = GameVersions.ResourceManager;
        var resourceSet = resourceManager.GetResourceSet(CultureInfo.InvariantCulture, true, true);

        if (resourceSet != null)
        {
            foreach (System.Collections.DictionaryEntry entry in resourceSet)
            {
                if (entry.Value is not string json)
                    continue;

                try
                {
                    var version = JsonSerializer.Deserialize<GameVersion>(json);
                    if (version == null)
                        continue;

                    var hashStr = version.Hash.StartsWith("0x", StringComparison.Ordinal) ? version.Hash[2..] : version.Hash;
                    var hash = ulong.Parse(hashStr, NumberStyles.HexNumber, CultureInfo.InvariantCulture);

                    byHash[hash] = version;

                    if (entry.Key is string name)
                    {
                        byName[name] = version;
                    }
                }
                catch (Exception ex) when (ex is JsonException or FormatException or OverflowException)
                {
                    Trace.TraceWarning($"Skipping invalid GameVersions.resx entry '{entry.Key}': {ex.Message}");
                }
            }
        }

        Versions = byHash;
        ByName = byName;
    }
}