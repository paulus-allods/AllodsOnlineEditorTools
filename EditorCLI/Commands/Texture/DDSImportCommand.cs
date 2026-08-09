using System.ComponentModel;
using AllodsOnlineEditorTools.ClientResources.Texture.DDS;
using JetBrains.Annotations;
using Spectre.Console.Cli;

namespace EditorCLI.Commands.Texture;

[UsedImplicitly]
internal sealed class DDSImportCommand : Command<DDSImportCommand.DDSImportCommandSettings>
{
    [UsedImplicitly]
    public class DDSImportCommandSettings : CommandSettings
    {
        [CommandArgument(0, "<.dds file>")]
        [Description("Path to DDS file")]
        public string File { get; set; } = string.Empty;
        [CommandOption("-o|--output <out>")]
        [Description("Output path for generated files")]
        public string OutputDirectory { get; set; } = string.Empty;
        [CommandOption("-m|--metadata")]
        [DefaultValue(true)]
        [Description("Generate jdb metadata file")]
        public bool GenerateMetadata { get; set; }
    }

    public override int Execute(CommandContext context, DDSImportCommandSettings settings, CancellationToken cancellationToken)
    {
        var dds = DDSTexture.LoadDDS(settings.File);
        return 0;
    }
}