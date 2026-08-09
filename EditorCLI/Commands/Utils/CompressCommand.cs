using System.ComponentModel;
using System.IO.Compression;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using Spectre.Console.Cli;

namespace EditorCLI.Commands.Utils;

[UsedImplicitly]
[Description("Compress a file using zlib")]
internal sealed class CompressCommand(ILogger<CompressCommand> logger)
    : Command<CompressCommand.CompressCommandSettings>
{
    [UsedImplicitly]
    public class CompressCommandSettings : CommandSettings
    {
        [CommandArgument(0, "<input>")]
        [Description("Path to the file to compress")]
        public string InputPath { get; set; } = string.Empty;

        [CommandOption("-o|--output <out>")]
        [Description("Output path for the compressed file (defaults to <input>.z)")]
        public string? OutputPath { get; set; }

        [CommandOption("--compression-level <level>")]
        [Description("zlib compression level from 0 (no compression) to 9 (maximum compression)")]
        [DefaultValue(6)]
        public int CompressionLevel { get; set; }

        public override Spectre.Console.ValidationResult Validate()
        {
            if (CompressionLevel is < 0 or > 9)
            {
                return Spectre.Console.ValidationResult.Error("Compression level must be between 0 and 9");
            }

            return Spectre.Console.ValidationResult.Success();
        }
    }

    public override int Execute(CommandContext context, CompressCommandSettings settings, CancellationToken cancellationToken)
    {
        if (!File.Exists(settings.InputPath))
        {
            logger.LogError("Input file not found: {InputPath}", settings.InputPath);
            return 1;
        }

        var outputPath = settings.OutputPath ?? settings.InputPath + ".z";

        var options = new ZLibCompressionOptions { CompressionLevel = settings.CompressionLevel };

        using (var input = File.OpenRead(settings.InputPath))
        using (var output = File.Create(outputPath))
        using (var zlib = new ZLibStream(output, options, leaveOpen: true))
        {
            input.CopyTo(zlib);
        }

        logger.LogInformation("Compressed {InputPath} to {OutputPath} with compression level {Level}",
            settings.InputPath, outputPath, settings.CompressionLevel);

        return 0;
    }
}