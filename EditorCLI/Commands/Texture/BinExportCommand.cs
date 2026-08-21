using System.ComponentModel;
using AllodsOnlineEditorTools.ClientResources.Serialization;
using AllodsOnlineEditorTools.ClientResources.Serialization.Jdb;
using AllodsOnlineEditorTools.ClientResources.Texture;
using AllodsOnlineEditorTools.ClientResources.Texture.DDS;
using JetBrains.Annotations;
using SixLabors.ImageSharp;
using Spectre.Console.Cli;

namespace EditorCLI.Commands.Texture;

[UsedImplicitly]
internal sealed class BinExportCommand : Command<BinExportCommand.BinExportCommandSettings>
{
    [UsedImplicitly]
    public class BinExportCommandSettings : CommandSettings
    {
        [CommandArgument(0, "<.jdb file>")]
        [Description("Path to texture metadata file")]
        public string File { get; set; } = string.Empty;

        [CommandArgument(1, "<resources root>")]
        [Description("Path to root resources filesystem")]
        public string ResourcesRoot { get; set; } = string.Empty;

        [CommandOption("-o|--output <out>")]
        [Description("Output path for generated files")]
        [DefaultValue("Unpack")]
        public string OutputDirectory { get; set; } = string.Empty;

        [CommandOption("-f|--format <fmt>")]
        [Description("Output file format")]
        public OutFormat Format { get; set; }

        public enum OutFormat
        {
            DDS,
            PNG
        }
    }

    protected override int Execute(CommandContext context, BinExportCommandSettings settings, CancellationToken cancellationToken)
    {
        var jsonSerializer = new JdbStructSerializer(JdbStructSerializerOptions.Default, ResourceSerializationContext.Default);
        var metadata = jsonSerializer.ParseResource(File.ReadAllText(settings.File), out _);

        if (metadata is not ITexture texture)
        {
            throw new InvalidDataException($"{settings.File} is not a valid Texture metadata");
        }

        var binaryFilePath = Path.Combine(settings.ResourcesRoot, texture.GetFilePath());
        using var textureFile = File.OpenRead(binaryFilePath);

        var basePath = Path.Combine(settings.OutputDirectory, texture.GetFilePath());
        var baseDirectory = Path.GetDirectoryName(basePath);
        if (!string.IsNullOrEmpty(baseDirectory))
        {
            Directory.CreateDirectory(baseDirectory);
        }

        switch (settings.Format)
        {
            case BinExportCommandSettings.OutFormat.DDS:
                var dds = DDSTexture.LoadBin(textureFile, texture);
                dds.SaveAsDDS(Path.ChangeExtension(basePath, ".dds"));
                break;
            case BinExportCommandSettings.OutFormat.PNG:
                var png = Image.FromBinTexture(textureFile, texture);
                png.SaveAsPng(Path.ChangeExtension(basePath, ".png"));
                break;
        }

        return 0;
    }
}
