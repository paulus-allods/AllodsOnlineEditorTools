using System.ComponentModel;
using System.IO.Compression;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using Spectre.Console.Cli;

namespace EditorCLI.Commands.Utils;

[UsedImplicitly]
[Description("Decompress a zlib compressed file")]
internal sealed class DecompressCommand(ILogger<DecompressCommand> logger)
    : Command<DecompressCommand.DecompressCommandSettings>
{
    [UsedImplicitly]
    public class DecompressCommandSettings : CommandSettings
    {
        [CommandArgument(0, "<input>")]
        [Description("Path to the zlib compressed file")]
        public string InputPath { get; set; } = string.Empty;

        [CommandOption("-o|--output <out>")]
        [Description("Output path for the decompressed file (defaults to <input> without its .z extension)")]
        public string? OutputPath { get; set; }
    }

    public override int Execute(CommandContext context, DecompressCommandSettings settings,
        CancellationToken cancellationToken)
    {
        if (!File.Exists(settings.InputPath))
        {
            logger.LogError("Input file not found: {InputPath}", settings.InputPath);
            return 1;
        }

        var outputPath = settings.OutputPath ?? ResolveOutputPath(settings.InputPath);

        using var decompressed = new MemoryStream();

        try
        {
            using var input = File.OpenRead(settings.InputPath);

            LogCompressionLevel(input);

            using var zlib = new ZLibStream(input, CompressionMode.Decompress);
            zlib.CopyTo(decompressed);
        }
        catch (InvalidDataException ex)
        {
            logger.LogError(
                "Failed to decompress {InputPath}: {Message}. The file does not appear to be a zlib stream.",
                settings.InputPath, ex.Message);
            return 1;
        }

        decompressed.Seek(0, SeekOrigin.Begin);
        using (var output = File.Create(outputPath))
        {
            decompressed.CopyTo(output);
        }

        logger.LogInformation("Decompressed {InputPath} to {OutputPath}", settings.InputPath, outputPath);

        return 0;
    }

    // The zlib header stores the compression level in the top two bits (FLEVEL) of the
    // second header byte (FLG). See RFC 1950 section 2.2.
    private void LogCompressionLevel(Stream stream)
    {
        stream.ReadByte(); // CMF
        var flg = stream.ReadByte();
        stream.Seek(0, SeekOrigin.Begin);

        if (flg < 0)
        {
            logger.LogWarning("File is too small to contain a zlib header; cannot read compression level");
            return;
        }

        var flevel = (flg >> 6) & 0x3;
        var description = flevel switch
        {
            0 => "fastest algorithm",
            1 => "fast algorithm",
            2 => "default algorithm",
            3 => "maximum compression, slowest algorithm",
            _ => "unknown",
        };
        logger.LogInformation("File compression level (FLEVEL): {Flevel} ({Description})", flevel, description);
    }

    private static string ResolveOutputPath(string inputPath)
    {
        return inputPath.EndsWith(".z", StringComparison.OrdinalIgnoreCase)
            ? inputPath[..^2]
            : inputPath + ".decompressed";
    }
}
