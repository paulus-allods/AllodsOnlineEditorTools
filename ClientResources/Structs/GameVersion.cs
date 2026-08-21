using System.Diagnostics.CodeAnalysis;
using AllodsOnlineEditorTools.ClientResources.DataTypes;
using AllodsOnlineEditorTools.ClientResources.Serialization.Bin.Database;

namespace AllodsOnlineEditorTools.ClientResources.Structs;

public class GameVersion
{
    /// <summary>Root namespace every per-version struct namespace lives under.</summary>
    public static readonly string StructsNamespace = typeof(GameVersion).Namespace!;

    public string Name { get; private init; } = string.Empty;
    /// <summary>The version-specific namespace segment (e.g. <c>V1_1_02_0</c>), or empty if no structs are
    /// generated for this version yet.</summary>
    public string Namespace { get; private init; } = string.Empty;
    /// <summary>
    /// The on-disk pack layout to use when repacking this version. The reader does not consult it: it
    /// detects the format from the payload instead. Defaults to <see cref="DatabaseFormat.V1"/>.
    /// </summary>
    public DatabaseFormat DatabaseFormat { get; init; } = DatabaseFormat.V1;
    public FileRefKind FileRefKind { get; init; } = FileRefKind.None;
    private string Hash { get; init; } = string.Empty;

    public bool NeedPacks => FileRefKind == FileRefKind.PakFileRef;
    /// <summary>Whether struct definitions are generated for this version.</summary>
    public bool HasStructs => Namespace.Length != 0;
    /// <summary>The fully-qualified struct namespace for this version, or empty if none are generated yet.</summary>
    public string FullNamespace => HasStructs ? $"{StructsNamespace}.{Namespace}" : string.Empty;
    public override string ToString() => $"{Name} ({Hash})";

    /// <summary>Looks a version up by its raw header bytes.</summary>
    public static bool TryGetByVersion(byte[] version, [NotNullWhen(true)] out GameVersion? gameVersion) => Versions.TryGetValue(Convert.ToHexString(version), out gameVersion);

    /// <summary>
    /// Looks a version up by its <see cref="Namespace"/>, the form used whenever a version is named in a
    /// command argument. Versions sharing a namespace are interchangeable here: they resolve to the same
    /// structs and agree on <see cref="FileRefKind"/>, so any one of them may be returned.
    /// </summary>
    public static bool TryGetByNamespace(string versionNamespace, [NotNullWhen(true)] out GameVersion? gameVersion)
    {
        gameVersion = Versions.Values.FirstOrDefault(version =>
            version.HasStructs && string.Equals(version.Namespace, versionNamespace, StringComparison.OrdinalIgnoreCase));
        return gameVersion is not null;
    }

    /// <summary>
    /// The supported client versions.
    /// </summary>
    private static readonly GameVersion[] All =
    [
        new()
        {
            Name = "Allods Online 1.1.02.0", Hash = "5847DB469364493C", Namespace = nameof(V1_1_02_0), FileRefKind = FileRefKind.FileRef,
        },
        new()
        {
            Name = "Allods Online 1.1.04.44", Hash = "304C70AC5A6F33D0", FileRefKind = FileRefKind.FileRef,
        },
        new()
        {
            Name = "Allods Online 4.0.02.4X", Hash = "641AD1D48E1FD7EC", Namespace = nameof(V4_0_02_43), FileRefKind = FileRefKind.FileRef2,
        },
        new()
        {
            Name = "Allods Online 3.0.0.X", Hash = "2060B40B8CBE5B8D", Namespace = nameof(V3_0_00_89), FileRefKind = FileRefKind.FileRef2,
        },
        new()
        {
            Name = "Allods Online 7.0.00.7X", Hash = "7025BD5027724A6D", Namespace = nameof(V7_0_00_76), FileRefKind = FileRefKind.FileRef2,
        },
        new()
        {
            Name = "Cloud Pirates 1.7.7", Hash = "B077EC9F77A40AA0", FileRefKind = FileRefKind.None,
        },
        new()
        {
            Name = "Allods Online 14.0.00.21", Hash = "44068E78E8E67876", FileRefKind = FileRefKind.PakFileRef,
        },
        new()
        {
            Name = "Allods Online 14.0.01.71", Hash = "983A36AC75DB9EC5", Namespace = nameof(V14_0_01_71), FileRefKind = FileRefKind.PakFileRef,
        },
        new()
        {
            Name = "Allods Online 17.0.01.55", Hash = "C4022E5973040000441E2B41", FileRefKind = FileRefKind.PakFileRef, DatabaseFormat = DatabaseFormat.V2,
        },
    ];

    public static readonly IReadOnlyDictionary<string, GameVersion> Versions = All.ToDictionary(version => version.Hash, StringComparer.OrdinalIgnoreCase);

    /// <summary>The distinct namespaces accepted by version arguments.</summary>
    public static readonly IReadOnlyList<string> StructNamespaces = Versions.Values
        .Where(version => version.HasStructs)
        .Select(version => version.Namespace)
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToList();
}
