using System.ComponentModel;
using AllodsOnlineEditorTools.ClientResources.Serialization.Bin;
using AllodsOnlineEditorTools.ClientResources.Structs;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using Spectre.Console.Cli;

namespace EditorCLI.Commands.Pack;

[UsedImplicitly]
[Description("Display information about a packed database (version, structs, packs, texts, file count)")]
internal sealed class PackInfoCommand(IAnsiConsole console, ILoggerFactory loggerFactory)
    : Command<PackInfoCommand.PackInfoCommandSettings>
{
    [UsedImplicitly]
    public class PackInfoCommandSettings : CommandSettings
    {
        [Description("Path to Bin folder containing databases or path to pak archive containing Bin folder")]
        [CommandArgument(0, "<Bin>")]
        public string BinPath { get; set; } = string.Empty;

        [Description("Name of the .bin database to analyze (required when the Bin folder/pak contains more than one)")]
        [CommandArgument(1, "[File]")]
        public string? File { get; set; }

        [CommandOption("--version")]
        [Description(
            "Display the database hash version and matching version name (on by default; disable with --no-version)")]
        [DefaultValue(true)]
        public bool ShowVersion { get; set; }

        [CommandOption("--no-version")]
        [Description("Hide the database hash version")]
        public bool HideVersion { get; set; }

        [CommandOption("--structs")]
        [Description("Display the list of struct types serialized in the database")]
        [DefaultValue(false)]
        public bool ShowStructs { get; set; }

        [CommandOption("--packs")]
        [Description("Display the list of pak archives referenced by the database")]
        [DefaultValue(false)]
        public bool ShowPacks { get; set; }

        [CommandOption("--texts")]
        [Description("Display the list of referenced text files")]
        [DefaultValue(false)]
        public bool ShowTexts { get; set; }
    }

    public override int Execute(CommandContext context, PackInfoCommandSettings settings,
        CancellationToken cancellationToken)
    {
        var (metadata, _) = DatabaseLoader.LoadDatabases(settings.BinPath, loggerFactory);

        if (metadata.Count == 0)
        {
            console.MarkupLineInterpolated($"[red]info:[/] no databases found in '{settings.BinPath}'");
            return 1;
        }

        if (!TrySelectDatabase(metadata, settings.File, out var database))
        {
            return 1;
        }

        if (settings.ShowVersion && !settings.HideVersion)
        {
            var versionName = GameVersion.Versions.TryGetValue(database.Version, out var version)
                ? version.ToString()
                : "unknown";
            console.MarkupLineInterpolated($"[yellow]Version:[/] 0x{database.Version:X16} ({versionName})");
        }

        if (settings.ShowStructs)
        {
            var structs = database.Structs
                .Distinct()
                .OrderBy(s => s, StringComparer.OrdinalIgnoreCase)
                .ToList();
            console.MarkupLineInterpolated($"[yellow]Structs ({structs.Count}):[/]");
            foreach (var name in structs)
            {
                console.WriteLine(name);
            }
        }

        if (settings.ShowPacks)
        {
            if (database.Packs is { Count: > 0 } packs)
            {
                console.MarkupLineInterpolated($"[yellow]Packs ({packs.Count}):[/]");
                foreach (var pack in packs)
                {
                    console.WriteLine(pack);
                }
            }
            else
            {
                console.MarkupLine("[yellow]Packs:[/] database has no pack");
            }
        }

        if (settings.ShowTexts)
        {
            var texts = database.TextFileRefNames.Values
                .Where(t => !string.IsNullOrEmpty(t))
                .Distinct()
                .OrderBy(t => t, StringComparer.OrdinalIgnoreCase)
                .ToList();
            console.MarkupLineInterpolated($"[yellow]Text files ({texts.Count}):[/]");
            foreach (var text in texts)
            {
                console.WriteLine(text);
            }
        }

        console.MarkupLineInterpolated($"[yellow]Total files:[/] {database.Dbid2File.Count}");

        return 0;
    }

    private bool TrySelectDatabase(Dictionary<string, DatabaseMetadata> metadata, string? file,
        out DatabaseMetadata database)
    {
        if (string.IsNullOrEmpty(file))
        {
            if (metadata.Count == 1)
            {
                database = metadata.Values.First();
                return true;
            }

            console.MarkupLineInterpolated(
                $"[red]info:[/] input contains {metadata.Count} databases; specify which .bin to analyze");
            ListDatabases(metadata);
            database = null!;
            return false;
        }

        // Match the requested name, tolerating a missing .bin extension.
        var name = Path.GetExtension(file).Equals(".bin", StringComparison.OrdinalIgnoreCase) ? file : file + ".bin";
        var match = metadata.Keys.FirstOrDefault(k => k.Equals(name, StringComparison.OrdinalIgnoreCase));
        if (match is null)
        {
            console.MarkupLineInterpolated($"[red]info:[/] no database named '{file}' was found");
            ListDatabases(metadata);
            database = null!;
            return false;
        }

        database = metadata[match];
        return true;
    }

    private void ListDatabases(Dictionary<string, DatabaseMetadata> metadata)
    {
        console.MarkupLine("[yellow]Available databases:[/]");
        foreach (var name in metadata.Keys.OrderBy(k => k, StringComparer.OrdinalIgnoreCase))
        {
            console.WriteLine(name);
        }
    }
}
