using System.ComponentModel;
using System.Diagnostics;
using AllodsOnlineEditorTools.ClientResources.DataTypes;
using AllodsOnlineEditorTools.ClientResources.Structs;
#if !IS_OPEN_SOURCE_BUILD
using AllodsOnlineEditorTools.StructCodeGeneration;
#endif
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using Spectre.Console.Cli;

namespace EditorCLI.Commands.Generation;

[UsedImplicitly]
internal sealed class GenerateStructCodeCommand(IAnsiConsole console, ILogger<GenerateStructCodeCommand> logger, ILoggerFactory loggerFactory)
    : Command<GenerateStructCodeCommand.GenerateStructCodeCommandSettings>
{
    [UsedImplicitly]
    public class GenerateStructCodeCommandSettings : CommandSettings
    {
        [Description("Path to Bin folder containing databases or path to pak archive containing Bin folder")]
        [CommandArgument(0, "<Bin>")]
        public string BinPath { get; set; } = string.Empty;

        [Description("Namespace of the game version the generated code targets (e.g. V14_0_01_71)")]
        [CommandArgument(1, "<version>")]
        public string Version { get; init; } = string.Empty;

        [Description("Hex address of the metainfo pointer. If omitted (together with --register-metainfo), auto-discovery is used.")]
        [CommandOption("--metainfo <ADDR>")]
        public string? MetainfoPointer { get; init; }

        [Description("Hex address of the metainfo registrators array. If omitted (together with --metainfo), auto-discovery is used.")]
        [CommandOption("--register-metainfo <ADDR>")]
        public string? RegisterMetainfoArrayPointer { get; init; }

        [CommandOption("--output-dir")]
        [DefaultValue("output")]
        public string OutputDirectory { get; init; } = string.Empty;

        [CommandOption("--types-xml")]
        public string? TypesXmlFile { get; init; }

        [Description(
            "Struct names to generate (comma-separated or repeated). When provided, structs are taken from this list instead of being derived from the pack.bin databases.")]
        [CommandOption("--structs <NAMES>")]
        public string[] Structs { get; init; } = [];

        [Description("Generate the Animations enum from the SkeletalAnimation instances in the databases (merged with types.xml when provided).")]
        [CommandOption("--animations")]
        [DefaultValue(false)]
        public bool Animations { get; init; }

        [Description("Run generation without writing any output files.")]
        [CommandOption("--dry-run")]
        [DefaultValue(false)]
        public bool DryRun { get; init; }

        public override ValidationResult Validate()
        {
            var hasMeta = !string.IsNullOrWhiteSpace(MetainfoPointer);
            var hasReg = !string.IsNullOrWhiteSpace(RegisterMetainfoArrayPointer);
            return hasMeta != hasReg
                ? ValidationResult.Error("Either provide both --metainfo and --register-metainfo, or neither (for auto-discovery).")
                : ValidationResult.Success();
        }
    }

    protected override int Execute(CommandContext context, GenerateStructCodeCommandSettings settings, CancellationToken cancellationToken)
    {
#if IS_OPEN_SOURCE_BUILD
        logger.LogError(
            "Struct code generation is not included in the open-source build of AllodsOnlineEditorTools.");
        return 1;
#else
        var gameProcesses = Process.GetProcessesByName("AOGame");

        if (gameProcesses.Length == 0)
        {
            logger.LogError("No game process found, check that AOGame.exe is currently running !");
            return 1;
        }

        if (gameProcesses.Length > 1)
        {
            logger.LogWarning("More than one game process found, first one will be taken");
        }


        int? registerPtrAddr = string.IsNullOrWhiteSpace(settings.RegisterMetainfoArrayPointer)
            ? null
            : Convert.ToInt32(settings.RegisterMetainfoArrayPointer, 16);
        int? metainfoPtrAddr = string.IsNullOrWhiteSpace(settings.MetainfoPointer) ? null : Convert.ToInt32(settings.MetainfoPointer, 16);

        var structNames = settings.Structs.SelectMany(s => s.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)).Distinct()
            .ToArray();

        if (structNames.Length > 0)
        {
            logger.LogInformation("Generating {Count} struct(s) from the provided list instead of the databases", structNames.Length);
        }

        var collection = new StructCollector(loggerFactory).Collect(gameProcesses[0].Id, settings.BinPath, settings.Animations, registerPtrAddr,
            metainfoPtrAddr, structNames);

        if (collection is null)
        {
            return 1;
        }

        var version = settings.Version;
        var versionDir = $"{settings.OutputDirectory}/{version}/";

        if (!GameVersion.TryGetByNamespace(version, out var gameVersion))
        {
            logger.LogWarning("Namespace {Version} is not declared in GameVersion.cs; FileRef inference will be disabled", version);
        }

        var generator = new StructCodeGenerator(collection, settings.TypesXmlFile, version, gameVersion?.FileRefKind ?? FileRefKind.None, logger);

        if (settings.DryRun)
        {
            logger.LogInformation("Dry-run mode: no files will be written");
        }

        var enumDir = $"{versionDir}Enums/";

        var structGenerated = WriteTemplates(versionDir, "structs", generator.BuildStructTemplates(), t => t.Name, t => t.TransformText());
        logger.LogInformation("Struct code generation completed: {Number} struct generated", structGenerated);

        if (generator.EnumCount > 0)
        {
            var enumGenerated = WriteTemplates(enumDir, "enums", generator.BuildEnumTemplates(), t => t.Name, t => t.TransformText());
            logger.LogInformation("Enum code generation completed: {Number} enums generated", enumGenerated);
        }

        if (settings.Animations)
        {
            var template = generator.BuildAnimationsEnum();

            if (!settings.DryRun)
            {
                Directory.CreateDirectory(enumDir);
                File.WriteAllText($"{enumDir}{template.Name}.cs", template.TransformText());
            }

            logger.LogInformation("Animations enum generated: {Number} entries", template.Entries.Count());
        }

        return 0;

        int WriteTemplates<T>(string dir, string kind, IEnumerable<T> templates, Func<T, string> name, Func<T, string> render)
        {
            var count = 0;
            console.Status().Spinner(Spinner.Known.Ascii).Start($"Generating {kind}", _ =>
            {
                if (!settings.DryRun)
                {
                    Directory.CreateDirectory(dir);
                }

                foreach (var template in templates)
                {
                    var fileName = $"{name(template)}.cs";
                    if (!settings.DryRun)
                    {
                        File.WriteAllText($"{dir}{fileName}", render(template));
                    }

                    logger.LogDebug("Code generated: {FileName}", fileName);
                    count++;
                }
            });
            return count;
        }
#endif
    }
}
