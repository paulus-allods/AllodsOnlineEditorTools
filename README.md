# Allods Online Editor Tools

[![CI](https://github.com/paulus-allods/AllodsOnlineEditorTools/actions/workflows/ci.yml/badge.svg)](https://github.com/paulus-allods/AllodsOnlineEditorTools/actions/workflows/ci.yml)

Tooling for reading, converting, and inspecting the client resources of the MMO
[Allods Online](https://allods.ru/). It parses the game's binary databases and
textures and converts them into editable, human-readable formats.

> [!NOTE]
> This is an unofficial, fan-made project. It is not affiliated
> with or endorsed by Allods Team, my.games. It ships no game assets;
> you need your own copy of the game to use it.
>
> "Allods Online" and related names, logos, and marks are trademarks of their
> respective owners (Allods Team / my.games). They are used here only to
> identify the game this project is compatible with. This project claims no
> ownership of them and is not sponsored, endorsed, or affiliated with the
> rights holders.

## What it does

- **Unpack databases** : read the game's `.bin ` databases (from a folder or a
  `.pak` archive) and export them to `jdb` (JSON) or `xdb` (XML).
- **Cross-version casting** : unpack one client version but serialize it with
  another version's struct/enum definitions (`--as <version>`). Only
  compatible resources are extracted; see below.
- **Inspect databases** : list the files contained in the databases (with
  wildcard and struct-type filters) and show their metadata (version, structs,
  referenced packs, file count).
- **Convert textures** : export the game's `.bin` textures to `DDS` or `PNG`.
  (Importing `DDS` back is planned but not implemented yet.)
- **Compress / decompress** : zlib helpers for the game's compressed files.

### Supported versions
> [!IMPORTANT]
> Versions not mentioned in this table are **not** supported.


Each client version is identified by its database hash, so the right struct/enum
model is selected automatically when a database is opened. The authoritative list
lives in [`ClientResources/Structs/GameVersion.cs`](ClientResources/Structs/GameVersion.cs);
`EditorCLI info versions` prints it.

Wherever a command takes a version argument, the version is named by its struct
namespace (e.g. `V4_0_02_43`), not by its display name. Those namespaces are
listed by `EditorCLI info versions --namespaces` and in the `--help` of every
command that takes one.

| Game          | Version        | Support state                             |
|---------------|----------------|-------------------------------------------|
| Allods Online | `1.1.02.0`     | ✅ Supported                              |
| Allods Online | `3.0.0.x`      | ✅ Supported                              |
| Allods Online | `4.0.02.4x`    | ✅ Supported                              |
| Allods Online | `7.0.00.7x`    | ✅ Supported                              |
| Allods Online | `14.0.01.71`   | ✅ Supported                              |
| Cloud Pirates | `1.7.7`        | 🚧 Parsing only, no unpack                |
| Allods Online | `17.0.01.55`   | 🚧 Parsing only, no unpack                |


### Cross-version casting (`--as`)

`pack unpack --as <version>` unpacks the binary with the source version's
struct layout, then **casts** each resource to the target version's
definitions before serializing, so the output gets the target's enum names
and type model:

- Fields are matched by name. A struct present in both versions with
  identical fields casts fully; if some fields differ, only the matching
  fields are cast; a struct absent from the target is skipped entirely.
- Enum values are remapped **by entry name** when both versions define the
  enum; a source without enum info passes the raw value through.
- Enums that are version-specific without touching game mechanics (e.g.
  animation ids) can be marked `[EnumRef(..., UseSourceOnCast = true)]` so the
  source version's enum is kept instead of being remapped.
- `--strict` turns warnings into failures: the run aborts instead of skipping
  when anything cannot be cast. Outside casting, `--strict` likewise fails
  `pack unpack` when a database references a struct that has no implementation.

### JDB vs XDB output

Both formats carry the same data. **`jdb` (JSON) is the better fit for web
usage** and general tooling. It maps directly onto JSON parsers and is easy to
consume from JavaScript/browsers. `xdb` (XML) mirrors the game's original
editor format and is there mainly for compatibility.

## Projects

| Project                  | Description                                                              |
| ------------------------ |--------------------------------------------------------------------------|
| `ClientResources`        | Core library: struct model, data types, geometry, textures, serializers. |
| `EditorCLI`              | Command-line front end (Spectre.Console).                                |
| `ClientResources.Tests`  | NUnit tests, including struct-layout validation.                         |

## File format documentation

The [`doc/`](doc) folder documents the custom `.bin` database format:

- [`doc/bin-format.md`](doc/bin-format.md), a prose description of the format:
  its zlib compression, chunk framing, and every chunk.
- [`doc/allods_bin_v1.hexpat`](doc/allods_bin_v1.hexpat),
  [`doc/allods_bin_v2_x86.hexpat`](doc/allods_bin_v2_x86.hexpat) and
  [`doc/allods_bin_v2_x64.hexpat`](doc/allods_bin_v2_x64.hexpat),
  [ImHex](https://imhex.werwolv.net/) patterns you can apply to a decompressed
  database to explore its structure interactively, one per format version and
  client architecture.

## Requirements

- [.NET SDK 10.0](https://dotnet.microsoft.com/download) or later

## Build & run

Build the individual project files, **not the solution**: the solution also
references `StructCodeGeneration`, which is not part of this repository (see
the note below):

```sh
dotnet build EditorCLI/EditorCLI.csproj
EditorCLI --help
```

To produce a self-contained, single-file executable (here for `win-x64`):

```sh
dotnet publish EditorCLI/EditorCLI.csproj -c Release -r win-x64 --self-contained true \
  -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true
```

> [!WARNING]
> **Run in Release for normal use.** Debug builds enable extra `Debug.Assert`
> checks that guard the reverse-engineered format parsers. Some of these may
> trigger while unpacking real data and this is expected: they are development
> aids meant to surface format edge cases, not fatal errors. Running a
> **Release** build on a **supported version** (see the table above) should
> unpack cleanly.

## Usage

```sh
# Unpack databases from a Bin folder (or a .pak) to XDB
EditorCLI pack unpack <Bin> [Packs] -o Unpack

# Unpack, casting the resources to another version's structs/enums
EditorCLI pack unpack <Bin> [Packs] -o Unpack --as V4_0_02_43

# List the files inside the databases / show database metadata
# (<Bin> is a Bin folder or a .pak archive containing one)
EditorCLI pack ls <Bin> [path]
EditorCLI pack info <Bin>

# Export a texture to PNG (experimental)
EditorCLI texture bin export <texture.jdb> <resources-root> -o out -f PNG

# zlib compress / decompress
EditorCLI utils compress <input> -o <output>
EditorCLI utils decompress <input> -o <output>

# List the supported client versions (add --namespaces for the bare list
# of values accepted by --as and other version arguments)
EditorCLI info versions
```

Run any command with `--help` for its full set of options.

> [!NOTE]
> The `generate structs` command relies on an internal, non-public component that
> inspects a running game process to recover struct layouts. That component is
> **not part of this repository**, so `generate structs` is disabled in the
> open-source build and will report that it is unavailable. The generated struct
> model it produces is already checked in under `ClientResources/Structs`.

## Planned features

- **Repacking** : write changes back into `.bin` archives.
- **DDS texture import** : convert a `.dds` back into the game's texture
  format (the `texture dds import` command exists but is not functional yet).
- **Blender plugin integration** : import game geometry directly into Blender.
- **Godot engine plugin integration** : bring assets into Godot.
- **More asset conversion** ; additional formats such as sounds, maps, more texture
  types, and other resources.

Contributions toward any of these are very welcome.

## Contributing

Issues and pull requests are welcome. See [CONTRIBUTING.md](CONTRIBUTING.md).

## License

Released under the [MIT License](LICENSE): you may use, modify, and
redistribute it freely, including commercially, as long as the copyright and
license notice are preserved.
