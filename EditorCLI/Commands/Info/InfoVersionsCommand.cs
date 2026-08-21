using System.ComponentModel;
using AllodsOnlineEditorTools.ClientResources.Serialization;
using AllodsOnlineEditorTools.ClientResources.Structs;
using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;

namespace EditorCLI.Commands.Info;

[UsedImplicitly]
[Description("List the client versions supported by the tool")]
internal sealed class InfoVersionsCommand(IAnsiConsole console) : Command<InfoVersionsCommand.InfoVersionsCommandSettings>
{
    [UsedImplicitly]
    public class InfoVersionsCommandSettings : CommandSettings
    {
        [CommandOption("--namespaces")][Description("Print only the version namespaces accepted by version arguments")][DefaultValue(false)] public bool NamespacesOnly { get; init; }
    }

    public override int Execute(CommandContext context, InfoVersionsCommandSettings settings, CancellationToken cancellationToken)
    {
        if (settings.NamespacesOnly)
        {
            foreach (var versionNamespace in GameVersion.StructNamespaces)
            {
                console.WriteLine(versionNamespace);
            }

            return 0;
        }

        var table = new Table().Border(TableBorder.Rounded);
        table.AddColumn("Version");
        table.AddColumn("Namespace");
        table.AddColumn(new TableColumn("Structs").RightAligned());
        table.AddColumn("DB format");
        table.AddColumn("FileRef");

        foreach (var version in GameVersion.Versions.Values)
        {
            table.AddRow(
                new Markup(version.Name.EscapeMarkup()),
                version.HasStructs ? new Markup(version.Namespace.EscapeMarkup()) : new Markup("[dim]-[/]"),
                new Markup(StructCount(version)),
                new Markup(version.DatabaseFormat.ToString()),
                new Markup(version.FileRefKind.ToString()));
        }

        console.Write(table);
        return 0;
    }

    // A count rather than a yes/no: a namespace can be declared here while its structs are absent from the build.
    private static string StructCount(GameVersion version) => version.HasStructs
        ? StructTypeResolverCache.ForNamespace(version.FullNamespace).ByName.Count.ToString()
        : "[dim]-[/]";
}
