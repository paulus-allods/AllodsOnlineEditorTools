using System.ComponentModel;
using AllodsOnlineEditorTools.ClientResources.Serialization.Bin;
using AllodsOnlineEditorTools.ClientResources.Serialization.Bin.Database;
using AllodsOnlineEditorTools.ClientResources.Structs;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using Spectre.Console.Cli;

namespace EditorCLI.Commands.Pack;

[UsedImplicitly]
[Description("Display information about a packed database (version, structs, packs, texts, file count)")]
internal sealed class PackInfoCommand(IAnsiConsole console, ILoggerFactory loggerFactory) : Command<PackInfoCommand.PackInfoCommandSettings>
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

    protected override int Execute(CommandContext context, PackInfoCommandSettings settings, CancellationToken cancellationToken)
    {
        var databases = DatabaseLoader.LoadDatabases(settings.BinPath, loggerFactory);

        if (databases.Count == 0)
        {
            console.MarkupLineInterpolated($"[red]info:[/] no databases found in '{settings.BinPath}'");
            return 1;
        }

        if (!TrySelectDatabase(databases, settings.File, out var database))
        {
            return 1;
        }

        if (!settings.HideVersion)
        {
            var versionName = GameVersion.TryGetByVersion(database.Version, out var version) ? version.ToString() : "unknown";
            console.MarkupLineInterpolated($"[yellow]Version:[/] 0x{Convert.ToHexString(database.Version)} ({versionName})");
        }

        if (settings.ShowStructs)
        {
            var structs = database.Structs.Distinct().OrderBy(s => s, StringComparer.OrdinalIgnoreCase).ToList();
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
            if (database.TextFileRefNames is null)
            {
                console.MarkupLine("[yellow] Database has no texts");
            }
            else
            {
                var texts = database.TextFileRefNames.Values.Where(t => !string.IsNullOrEmpty(t)).Distinct().OrderBy(t => t, StringComparer.OrdinalIgnoreCase)
                    .ToList();
                console.MarkupLineInterpolated($"[yellow]Text files ({texts.Count}):[/]");
                foreach (var text in texts)
                {
                    console.WriteLine(text);
                }
            }
        }

        var rootCount = database.DbId2File.Count;
        var fileCount = database.ObjId2DbId is { } objIds ? $"{rootCount + objIds.Count} ({rootCount} root)" : $"{rootCount}";
        console.MarkupLineInterpolated($"[yellow]Total files:[/] {fileCount}");

        return 0;
    }

    private bool TrySelectDatabase(Dictionary<string, BinDatabase> databases, string? file, out DatabaseMetadata database)
    {
        if (string.IsNullOrEmpty(file))
        {
            if (databases.Count == 1)
            {
                database = databases.Values.First().Metadata;
                return true;
            }

            console.MarkupLineInterpolated($"[red]info:[/] input contains {databases.Count} databases; specify which .bin to analyze");
            ListDatabases(databases);
            database = null!;
            return false;
        }

        // Match the requested name, tolerating a missing .bin extension.
        var name = Path.GetExtension(file).Equals(".bin", StringComparison.OrdinalIgnoreCase) ? file : file + ".bin";
        var match = databases.Keys.FirstOrDefault(k => k.Equals(name, StringComparison.OrdinalIgnoreCase));
        if (match is null)
        {
            console.MarkupLineInterpolated($"[red]info:[/] no database named '{file}' was found");
            ListDatabases(databases);
            database = null!;
            return false;
        }

        database = databases[match].Metadata;
        return true;
    }

    private void ListDatabases(Dictionary<string, BinDatabase> databases)
    {
        console.MarkupLine("[yellow]Available databases:[/]");
        foreach (var name in databases.Keys.OrderBy(k => k, StringComparer.OrdinalIgnoreCase))
        {
            console.WriteLine(name);
        }
    }
}
