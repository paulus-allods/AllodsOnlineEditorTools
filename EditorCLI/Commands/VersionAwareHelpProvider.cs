using AllodsOnlineEditorTools.ClientResources.Structs;
using Spectre.Console;
using Spectre.Console.Cli;
using Spectre.Console.Cli.Help;
using Spectre.Console.Rendering;

namespace EditorCLI.Commands;

/// <summary>
/// Appends the list of version namespaces accepted by version arguments to the help of any command that
/// takes one. <see cref="System.ComponentModel.DescriptionAttribute"/> only carries constants, so the list
/// cannot be spelled out on the parameter itself.
/// </summary>
internal sealed class VersionAwareHelpProvider(ICommandAppSettings settings) : HelpProvider(settings)
{
    private const string VersionValueName = "version";

    public override IEnumerable<IRenderable> GetFooter(ICommandModel model, ICommandInfo? command)
    {
        var footer = base.GetFooter(model, command).ToList();

        if (command is null || !TakesVersion(command))
        {
            return footer;
        }

        // A single renderable: consecutive ones are concatenated inline rather than stacked.
        footer.Add(new Markup(
            $"\n[yellow]SUPPORTED VERSIONS:[/]\n    {string.Join(", ", GameVersion.StructNamespaces)}\n\n" +
            $"Run [blue]{model.ApplicationName.EscapeMarkup()} info versions[/] for details.\n"));

        return footer;
    }

    private static bool TakesVersion(ICommandInfo command) => command.Parameters.Any(parameter => parameter switch
    {
        ICommandOption option => IsVersionValue(option.ValueName),
        ICommandArgument argument => IsVersionValue(argument.Value),
        _ => false,
    });

    private static bool IsVersionValue(string? valueName) =>
        string.Equals(valueName, VersionValueName, StringComparison.OrdinalIgnoreCase);
}
