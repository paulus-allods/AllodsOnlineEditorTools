using System.ComponentModel;
using AllodsOnlineEditorTools.ClientResources.Serialization;
using AllodsOnlineEditorTools.ClientResources.Serialization.Bin;
using AllodsOnlineEditorTools.ClientResources.Serialization.Jdb;
using AllodsOnlineEditorTools.ClientResources.Serialization.Xdb;
using AllodsOnlineEditorTools.ClientResources.Structs;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using Spectre.Console.Cli;

namespace EditorCLI.Commands.Pack;

[UsedImplicitly]
[Description("Unpack bin databases to jdb or xdb files")]
internal sealed class UnpackCommand(ILogger<UnpackCommand> logger, ILoggerFactory loggerFactory)
    : Command<UnpackCommand.UnpackCommandSettings>
{
    [UsedImplicitly]
    public class UnpackCommandSettings : CommandSettings
    {
        [Description("Path to Bin folder containing databases or path to pak archive containing Bin folder")]
        [CommandArgument(0, "<Bin>")]
        public string BinPath { get; set; } = string.Empty;
        [CommandArgument(1, "[Packs]")]
        [Description("Path to folder containing game data pak files")]
        public string? PacksDirectory { get; set; }
        [CommandOption("-o|--output <out>")]
        [DefaultValue("Unpack")]
        [Description("Output path for unpacked files")]
        public string OutputDirectory { get; set; } = string.Empty;
        [CommandOption("-f|--format <fmt>")]
        [Description("Output format for unpacked files")]
        [DefaultValue(OutputFormat.Xdb)]
        public OutputFormat Format { get; init; }
        [CommandOption("--dry-run")]
        [Description("Enable dry run that does not write files to disk")]
        public bool Dry { get; set; }
        [CommandOption("--strict")]
        [Description("Fail if any struct referenced by the databases has no implementation")]
        public bool Strict { get; set; }
        [CommandOption("--as <version>")]
        [Description("Cast resources to another game version before serializing, incompatible resources/fields are skipped with a warning")]
        public string? CastToVersion { get; set; }
    }

    public override int Execute(CommandContext context, UnpackCommandSettings settings, CancellationToken cancellationToken)
    {
        var (metadata, data) = DatabaseLoader.LoadDatabases(settings.BinPath, loggerFactory);

        if (!metadata.TryGetValue("pack.bin", out var mainBinaryPackedDatabaseMetadata))
        {
            throw new InvalidDataException($"No pack.bin database found in '{settings.BinPath}'; cannot unpack without the main database");
        }

        if (!GameVersion.Versions.TryGetValue(mainBinaryPackedDatabaseMetadata.Version, out var version))
        {
            throw new NotSupportedException($"Unsupported version: {mainBinaryPackedDatabaseMetadata.Version:X}");
        }

        PacksRegistry? packsRegistry = null;
        if (version.NeedPacks)
        {
            if (settings.PacksDirectory is null)
            {
                throw new ArgumentException("Packs directory is required for this version");
            }

            logger.LogInformation("Loading packs from {PacksDirectory}", settings.PacksDirectory);
            packsRegistry = PacksRegistry.Load(settings.PacksDirectory);
        }

        logger.LogInformation("Loading structs for version {version}", version.ToString());

        var typeResolver = InitStructs(metadata, version, settings.Strict);

        var caster = settings.CastToVersion is null ? null : CreateCaster(settings.CastToVersion, version, metadata, settings.Strict);

        if (!settings.Dry) Directory.CreateDirectory(settings.OutputDirectory);

        int totalFiles = metadata.Values.Sum(m => m.Dbid2File.Count);

        logger.LogInformation("Start unpacking {TotalFiles} files", totalFiles);

        var extension = settings.Format.ToString().ToLowerInvariant();
        var binaryOptions = BinarySerializerOptions.Default;

        var processedFiles = 0;
        var lastLoggedDecile = 0;
        var progressLock = new Lock();

        void ReportProgress()
        {
            var done = Interlocked.Increment(ref processedFiles);
            var decile = (int)(done * 10L / totalFiles);
            if (decile <= lastLoggedDecile) return;
            lock (progressLock)
            {
                if (decile <= lastLoggedDecile) return;
                lastLoggedDecile = decile;
                logger.LogInformation("Unpacked {Processed}/{Total} files ({Percent}%)", done, totalFiles, decile * 10);
            }
        }

        foreach (var entry in metadata)
        {
            var serializerContext = new BinaryStructSerializerContext()
            {
                CurrentDatabaseMetadata = entry.Value,
                MainDatabaseMetadata = mainBinaryPackedDatabaseMetadata,
                TypeResolver = typeResolver,
                FileRefKind = version.FileRefKind,
                Packs = packsRegistry,
                LoggerFactory = loggerFactory,
            };

            var resourceContext = new ResourceSerializationContext
            {
                EnumRefOverrides = caster?.EnumRefOverrides,
            };
            var serializer = CreateSerializer(settings.Format, resourceContext, loggerFactory);

            Parallel.ForEach(entry.Value.Dbid2File, fileEntry =>
            {
                if (caster is not null)
                {
                    var structName = entry.Value.GetStructType(fileEntry.Key);
                    if (structName is null || !caster.CanCast(structName))
                    {
                        ReportProgress();
                        return;
                    }
                }

                using (logger.BeginScope("Database:{Database} File:{File}", entry.Key, fileEntry.Value))
                {
                    var result = BinaryStructSerializer.Deserialize(data[entry.Key], fileEntry.Key, serializerContext, binaryOptions);
                    if (caster is not null)
                    {
                        result = caster.Cast(result, resourceContext);
                    }

                    entry.Value.Dbid2Resid.TryGetValue(fileEntry.Key, out int resourceId);
                    var content = serializer.SerializeResource(result, resourceId);

                    if (!settings.Dry)
                    {
                        var directoryName = Path.GetDirectoryName(fileEntry.Value) ?? throw new InvalidOperationException($"Directory name is null for path {fileEntry.Value}");
                        Directory.CreateDirectory(Path.Combine(settings.OutputDirectory, directoryName));
                        var path = Path.ChangeExtension(Path.Combine(settings.OutputDirectory, fileEntry.Value), extension);
                        File.WriteAllText(path, content);
                    }
                }

                ReportProgress();
            });
        }

        return 0;
    }

    private static IResourceWriter CreateSerializer(OutputFormat format, ResourceSerializationContext context, ILoggerFactory loggerFactory) => format switch
    {
        OutputFormat.Jdb => new JdbStructSerializer(JdbStructSerializerOptions.Default, context, loggerFactory.CreateLogger<JdbStructSerializer>()),
        OutputFormat.Xdb => new XdbStructSerializer(XdbStructSerializerOptions.Default, context, loggerFactory.CreateLogger<XdbStructSerializer>()),
        _ => throw new NotSupportedException($"Unsupported output format: {format}"),
    };

    private StructCaster CreateCaster(string targetVersionName, GameVersion sourceVersion, Dictionary<string, DatabaseMetadata> metadata, bool strictMode)
    {
        if (!GameVersion.ByName.TryGetValue(targetVersionName, out var targetVersion))
        {
            throw new ArgumentException($"Unknown cast target version '{targetVersionName}'; known versions: {string.Join(", ", GameVersion.ByName.Keys)}");
        }

        var targetStructs = StructTypeResolver.FromVersion(targetVersion).ByName;
        if (targetStructs.Count == 0)
        {
            throw new InvalidOperationException($"Cast target version '{targetVersionName}' has no compiled structs");
        }

        logger.LogInformation("Casting resources from {Source} to {Target}", sourceVersion, targetVersion);

        var caster = new StructCaster(StructTypeResolver.FromVersion(sourceVersion).ByName, targetStructs, loggerFactory.CreateLogger<StructCaster>());
        caster.Analyze(metadata.Values.SelectMany(m => m.Structs).Distinct());

        if (caster.IncompatibilityCount > 0)
        {
            if (strictMode)
            {
                throw new InvalidOperationException($"{caster.IncompatibilityCount} struct(s)/field(s) cannot be cast to '{targetVersionName}' (strict mode)");
            }
            logger.LogWarning("{Count} struct(s)/field(s) cannot be cast to {Target} and will be skipped", caster.IncompatibilityCount, targetVersionName);
        }

        return caster;
    }

    private StructTypeResolver InitStructs(Dictionary<string, DatabaseMetadata> metadata, GameVersion allodsGameVersion, bool strictMode)
    {
        var typeResolver = StructTypeResolver.FromVersion(allodsGameVersion, loggerFactory.CreateLogger<StructTypeResolver>());

        var structs = metadata.Values.SelectMany(m => m.Structs).ToHashSet();
        var missingStructs = structs.Except(typeResolver.Types.Select(s => s.Name)).ToList();

        foreach (var missingStruct in missingStructs)
        {
            logger.LogWarning("Missing struct definition, will not unpack: {MissingStruct}", missingStruct);
        }

        if (missingStructs.Count > 0 && strictMode)
        {
            throw new InvalidOperationException($"{missingStructs.Count} struct(s) referenced by the databases have no implementation (strict mode)");
        }

        return typeResolver;
    }
}