using System.ComponentModel;
using System.Text;
using System.Text.RegularExpressions;
using AllodsOnlineEditorTools.ClientResources.Serialization.Bin;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging.Abstractions;
using Spectre.Console;
using Spectre.Console.Cli;

namespace EditorCLI.Commands.Pack;

[UsedImplicitly]
[Description("List files inside a packed database")]
internal sealed class PackListCommand(IAnsiConsole console) : Command<PackListCommand.PackListCommandSettings>
{
    [UsedImplicitly]
    public class PackListCommandSettings : CommandSettings
    {
        [Description("Path to Bin folder containing databases or path to pak archive containing Bin folder")]
        [CommandArgument(0, "<Bin>")]
        public string BinPath { get; set; } = string.Empty;

        [Description("Path inside the database to list; supports * and ? wildcards in the last segment (e.g. Textures, textures/toto, Textures/Toto/Tata*)")]
        [CommandArgument(1, "[Path]")]
        public string? Path { get; set; }

        [Description("Only list files of these struct types, comma-separated and case-insensitive (e.g. Geometry,Texture,UIGameRoot). Directories are shown when they contain a matching file")]
        [CommandOption("-t|--type <types>")]
        public string? Types { get; set; }
    }

    public override int Execute(CommandContext context, PackListCommandSettings settings, CancellationToken cancellationToken)
    {
        var (metadata, _) = DatabaseLoader.LoadDatabases(settings.BinPath, NullLoggerFactory.Instance);

        var typeFilter = settings.Types?
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var tree = new DirectoryTree();
        foreach (var meta in metadata.Values)
        {
            foreach (var (dbid, path) in meta.Dbid2File)
            {
                if (typeFilter is not null && !typeFilter.Contains(meta.GetStructType(dbid) ?? string.Empty))
                {
                    continue;
                }
                tree.Add(path);
            }
        }

        if (!tree.TryList(settings.Path, out var entries))
        {
            console.MarkupLineInterpolated($"[red]ls:[/] cannot access '{settings.Path}': no such file or directory");
            return 1;
        }
        
        foreach (var entry in entries.OrderByDescending(e => e.IsDirectory).ThenBy(e => e.Name, StringComparer.OrdinalIgnoreCase))
        {
            if (entry.IsDirectory)
            {
                console.MarkupLineInterpolated($"[blue]{entry.Name}/[/]");
            }
            else
            {
                console.WriteLine(entry.Name);
            }
        }

        return 0;
    }

    private readonly record struct Entry(string Name, bool IsDirectory);
    
    private sealed class DirectoryTree
    {
        private readonly Dictionary<string, Dictionary<string, bool>> _children = new(StringComparer.OrdinalIgnoreCase);

        public void Add(string path)
        {
            var segments = path.Replace('\\', '/').Split('/', StringSplitOptions.RemoveEmptyEntries);

            for (var i = 0; i < segments.Length; i++)
            {
                var parent = string.Join('/', segments.Take(i));
                var isDirectory = i < segments.Length - 1;

                if (!_children.TryGetValue(parent, out var childrenOfParent))
                {
                    childrenOfParent = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
                    _children[parent] = childrenOfParent;
                }

                childrenOfParent[segments[i]] = childrenOfParent.GetValueOrDefault(segments[i]) || isDirectory;
            }
        }

        public bool TryList(string? query, out IReadOnlyCollection<Entry> entries)
        {
            var normalized = (query ?? string.Empty).Replace('\\', '/').Trim('/');
            
            if (normalized.Length == 0)
            {
                return TryListDirectory(string.Empty, out entries);
            }

            var segments = normalized.Split('/');
            var leaf = segments[^1];
            var parent = string.Join('/', segments[..^1]);

            if (leaf.Contains('*') || leaf.Contains('?'))
            {
                return TryGlob(parent, leaf, out entries);
            }

            // An exact directory: list its contents (e.g. "Textures", "textures/toto").
            if (TryListDirectory(normalized, out entries))
            {
                return true;
            }

            // Otherwise it may be a single file: echo it back.
            if (_children.TryGetValue(parent, out var siblings))
            {
                var match = siblings.Keys.FirstOrDefault(name => name.Equals(leaf, StringComparison.OrdinalIgnoreCase));
                if (match is not null)
                {
                    entries = [new Entry(match, siblings[match])];
                    return true;
                }
            }

            entries = [];
            return false;
        }

        private bool TryListDirectory(string directory, out IReadOnlyCollection<Entry> entries)
        {
            if (_children.TryGetValue(directory, out var childrenOfDir))
            {
                entries = childrenOfDir.Select(c => new Entry(c.Key, c.Value)).ToList();
                return true;
            }

            entries = [];
            return false;
        }

        private bool TryGlob(string directory, string pattern, out IReadOnlyCollection<Entry> entries)
        {
            if (!_children.TryGetValue(directory, out var childrenOfDir))
            {
                entries = [];
                return false;
            }

            var regex = GlobToRegex(pattern);
            entries = childrenOfDir
                .Where(c => regex.IsMatch(c.Key))
                .Select(c => new Entry(c.Key, c.Value))
                .ToList();
            return true;
        }

        private static Regex GlobToRegex(string glob)
        {
            var builder = new StringBuilder("^");
            foreach (var c in glob)
            {
                builder.Append(c switch
                {
                    '*' => ".*",
                    '?' => ".",
                    _ => Regex.Escape(c.ToString())
                });
            }
            builder.Append('$');
            return new Regex(builder.ToString(), RegexOptions.IgnoreCase);
        }
    }
}