// Re-applies manual type overrides onto freshly regenerated struct files.
//
// After regenerating the ClientResources struct files, string fields the
// generator could not classify come back as `string` with a
// `//TODO: possible TextFileRef/FileRef*` comment. If a previous (committed)
// version of the file had already resolved that field to a concrete type
// (FileRef, TextFileRef, ...), this script restores that type and drops the
// TODO comment. String arrays are also restored: the generator never infers
// FileRef types for arrays (they come back as plain `string[]` with no TODO
// marker), so a `string[]` field whose previous type was a FileRef-family
// array (FileRef[], TextFileRef[]) gets that type
// back. Enum links are restored the same way: without types.xml, enum fields
// come back as `int`/`int[]` with a `//TODO: ENUM` comment; if the previous
// version carried an `[EnumRef(...)]` attribute on that field, the attribute
// is re-inserted verbatim (preserving e.g. `UseSourceOnCast = true`) and the
// TODO comment is dropped. Only those three field shapes are touched; every
// other line is left byte-for-byte unchanged. Matching is scoped to the
// enclosing class, so `name` in one nested class is never confused with
// `name` in another. The previous version is read from git (default: HEAD).
//
// Cross-platform, no project required (needs .NET SDK 10+):
//   dotnet run scripts/RestoreStructTypeOverrides.cs
//   dotnet run scripts/RestoreStructTypeOverrides.cs -- --dry-run
//   dotnet run scripts/RestoreStructTypeOverrides.cs -- --ref HEAD ClientResources/Structs/V1_1_04_44

using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

var paths = new List<string>();
string gitRef = "HEAD";
bool dryRun = false;

for (int i = 0; i < args.Length; i++)
{
    switch (args[i])
    {
        case "--ref" or "-r":
            gitRef = args[++i];
            break;
        case "--dry-run" or "-n":
            dryRun = true;
            break;
        case "--help" or "-h":
            Console.WriteLine("usage: dotnet run RestoreStructTypeOverrides.cs -- [--ref <ref>] [--dry-run] [path...]");
            return 0;
        default:
            paths.Add(args[i]);
            break;
    }
}
if (paths.Count == 0)
    paths.Add("ClientResources/Structs");

var classRe = new Regex(@"^public\s+(?:partial\s+)?class\s+(\w+)");
var fieldRe = new Regex(@"^\[FieldOffset\((\d+)\)\].*\bpublic\s+(\S+)\s+(@?\w+);");
var todoRe = new Regex(@"^//TODO: possible TextFileRef/FileRef");
var enumTodoRe = new Regex(@"^//TODO: ENUM\b");
var typeRe = new Regex(@"(public\s+)string(\s+@?\w+\s*;)");
var arrayTypeRe = new Regex(@"(public\s+)string\[\](\s+@?\w+\s*;)");
// Lazy body still swallows nested parens: the first ")]" only occurs at the attribute's end.
var enumRefRe = new Regex(@"\[EnumRef\(.*?\)\]");
// Arrays carry no TODO marker, so only restore types that are unambiguously
// manual FileRef-family resolutions of a generated string[].
var fileRefArrayTypes = new HashSet<string> { "FileRef[]", "TextFileRef[]" };

var (rootCode, repoRoot) = RunGit(".", "rev-parse", "--show-toplevel");
if (rootCode != 0)
{
    Console.Error.WriteLine("error: not inside a git repository");
    return 1;
}
repoRoot = repoRoot.Trim();

// Expand path arguments into a concrete list of .cs files.
var files = new List<string>();
foreach (var p in paths)
{
    var full = Path.IsPathRooted(p) ? p : Path.Combine(repoRoot, p);
    if (Directory.Exists(full))
        files.AddRange(Directory.EnumerateFiles(full, "*.cs", SearchOption.AllDirectories));
    else if (File.Exists(full))
        files.Add(Path.GetFullPath(full));
    else
        Console.Error.WriteLine($"warning: path not found: {p}");
}
files = files.Distinct().OrderBy(f => f).ToList();

int totalChanges = 0;

foreach (var file in files)
{
    var rel = Path.GetRelativePath(repoRoot, file).Replace('\\', '/');

    // Previous (resolved) version from git. Missing => new file, nothing to restore.
    var (showCode, oldText) = RunGit(repoRoot, "show", $"{gitRef}:{rel}");
    if (showCode != 0)
        continue;

    var oldMap = ResolvedFieldTypes(SplitLines(oldText));

    // Read the working (regenerated) file, preserving BOM and line endings.
    var bytes = File.ReadAllBytes(file);
    bool hasBom = bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF;
    var raw = new UTF8Encoding(false).GetString(hasBom ? bytes[3..] : bytes);
    var nl = raw.Contains("\r\n") ? "\r\n" : "\n";
    var lines = SplitLines(raw);

    var outLines = new List<string>();
    var stack = new Stack<string>();
    string? pending = null;
    var changes = new List<string>();

    foreach (var line in lines)
    {
        var trim = line.Trim();

        var cls = classRe.Match(trim);
        if (cls.Success) { pending = cls.Groups[1].Value; outLines.Add(line); continue; }
        if (trim == "{") { stack.Push(pending ?? ""); pending = null; outLines.Add(line); continue; }
        if (trim == "}") { if (stack.Count > 0) stack.Pop(); outLines.Add(line); continue; }

        var fld = fieldRe.Match(trim);
        if (fld.Success && fld.Groups[2].Value == "string")
        {
            var name = fld.Groups[3].Value;
            var scope = string.Join("/", stack);
            if (oldMap.TryGetValue($"{scope}::{name}", out var old) && old.Type != "string")
            {
                bool prevIsTodo = outLines.Count > 0 && todoRe.IsMatch(outLines[^1].Trim());
                if (prevIsTodo)
                {
                    var newLine = typeRe.Replace(line, m => m.Groups[1].Value + old.Type + m.Groups[2].Value, 1);
                    outLines.RemoveAt(outLines.Count - 1); // drop the //TODO line
                    outLines.Add(newLine);
                    changes.Add($"  {scope}::{name,-24} string -> {old.Type}");
                    continue;
                }
            }
        }
        else if (fld.Success && fld.Groups[2].Value == "string[]")
        {
            var name = fld.Groups[3].Value;
            var scope = string.Join("/", stack);
            if (oldMap.TryGetValue($"{scope}::{name}", out var old) && fileRefArrayTypes.Contains(old.Type))
            {
                var newLine = arrayTypeRe.Replace(line, m => m.Groups[1].Value + old.Type + m.Groups[2].Value, 1);
                outLines.Add(newLine);
                changes.Add($"  {scope}::{name,-24} string[] -> {old.Type}");
                continue;
            }
        }
        else if (fld.Success && fld.Groups[2].Value is "int" or "int[]")
        {
            var name = fld.Groups[3].Value;
            var scope = string.Join("/", stack);
            // Restore only where the generator flagged the field (//TODO: ENUM) and the previous
            // version had linked it to an enum of the same underlying shape.
            if (oldMap.TryGetValue($"{scope}::{name}", out var old)
                && old.EnumRef is not null
                && old.Type == fld.Groups[2].Value)
            {
                bool prevIsEnumTodo = outLines.Count > 0 && enumTodoRe.IsMatch(outLines[^1].Trim());
                if (prevIsEnumTodo)
                {
                    // Re-insert the attribute verbatim before ` public`, preserving any extra
                    // arguments (e.g. UseSourceOnCast = true).
                    var newLine = line.Insert(line.LastIndexOf(" public ", StringComparison.Ordinal), old.EnumRef);
                    outLines.RemoveAt(outLines.Count - 1); // drop the //TODO: ENUM line
                    outLines.Add(newLine);
                    changes.Add($"  {scope}::{name,-24} {old.Type} -> {old.EnumRef}");
                    continue;
                }
            }
        }

        outLines.Add(line);
    }

    if (changes.Count == 0)
        continue;

    totalChanges += changes.Count;
    Console.WriteLine($"{rel} ({changes.Count} restored)");
    changes.ForEach(Console.WriteLine);

    if (!dryRun)
        File.WriteAllText(file, string.Join(nl, outLines), new UTF8Encoding(hasBom));
}

Console.WriteLine();
Console.WriteLine(dryRun
    ? $"{totalChanges} field(s) would be restored (dry-run)."
    : $"{totalChanges} field(s) restored.");
return 0;

// Build a map of "ClassPath::fieldName" -> (resolved type, [EnumRef(...)] attribute) from source lines.
Dictionary<string, (string Type, string? EnumRef)> ResolvedFieldTypes(IEnumerable<string> lines)
{
    var map = new Dictionary<string, (string Type, string? EnumRef)>();
    var stack = new Stack<string>();
    string? pending = null;

    foreach (var line in lines)
    {
        var trim = line.Trim();
        var cls = classRe.Match(trim);
        if (cls.Success) { pending = cls.Groups[1].Value; continue; }
        if (trim == "{") { stack.Push(pending ?? ""); pending = null; continue; }
        if (trim == "}") { if (stack.Count > 0) stack.Pop(); continue; }
        var fld = fieldRe.Match(trim);
        if (fld.Success)
        {
            var scope = string.Join("/", stack);
            var enumRef = enumRefRe.Match(trim);
            map[$"{scope}::{fld.Groups[3].Value}"] = (fld.Groups[2].Value, enumRef.Success ? enumRef.Value : null);
        }
    }
    return map;
}

static string[] SplitLines(string text) => Regex.Split(text, "\r?\n");

static (int ExitCode, string StdOut) RunGit(string workingDir, params string[] gitArgs)
{
    var psi = new ProcessStartInfo("git")
    {
        WorkingDirectory = workingDir,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        UseShellExecute = false,
    };
    foreach (var a in gitArgs)
        psi.ArgumentList.Add(a);

    using var proc = Process.Start(psi)!;
    var stdout = proc.StandardOutput.ReadToEnd();
    proc.WaitForExit();
    return (proc.ExitCode, stdout);
}