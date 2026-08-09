# Allods Online Editor Tools

[![CI](https://github.com/paulus-allods/AllodsOnlineEditorTools/actions/workflows/ci.yml/badge.svg)](https://github.com/paulus-allods/AllodsOnlineEditorTools/actions/workflows/ci.yml)

Tooling for reading, converting, and inspecting the client resources of the MMO
[Allods Online](https://allods.ru/). It parses the game's binary databases and
textures and converts them into editable, human-readable formats.

> **Disclaimer** — This is an unofficial, fan-made project. It is not affiliated
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

Each client version is identified by its database hash, so the right struct/enum
model is selected automatically when a database is opened. The authoritative list
lives in [`ClientResources/GameVersions.resx`](ClientResources/GameVersions.resx).

| Game          | Version        | Support state                                |
|---------------|----------------|----------------------------------------------|
| Allods Online | `1.1.02.0`     | ✅ Supported                                  |
| Allods Online | `3.0.0.x`      | ✅ Supported                                  |
| Allods Online | `4.0.02.4x`    | ✅ Supported                                  |
| Allods Online | `7.0.00.7x`    | ✅ Supported                                  |
| Allods Online | `14.0.01.71`   | 🚧 Supported but XDB formatting is unfinished |
| Cloud Pirates | `1.7.7`        | ❌ Parsing only, assets export is planned     |
| Allods Online | `15.0 -> 17.0` | ❌ Planned                                    |

> **⚠️ Important — bin database format changed in 15.0**
> The structure of the `.bin` databases changed significantly starting with
> client version **15.0** and is **not currently supported**. Unpacking targets
> the earlier database format; support for the 15.0+ layout is planned but not
> yet implemented.

### Cross-version casting (`--as`)

Enum names can only be recovered for a version when its `types.xml` is
available. For versions where it is not, `pack unpack --as <version>` unpacks
the binary with the source version's struct layout, then **casts** each
resource to the target version's definitions before serializing — so the
output gets the target's enum names and type model:

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

- [`doc/bin-format.md`](doc/bin-format.md) — a prose description of the format:
  its zlib compression, chunk framing, and every chunk.
- [`doc/allods_bin.hexpat`](doc/allods_bin.hexpat) — an
  [ImHex](https://imhex.werwolv.net/) pattern you can apply to a decompressed
  database to explore its structure interactively.

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
```

Run any command with `--help` for its full set of options.

## Note on struct code generation

The `generate structs` command relies on an internal, non-public component that
inspects a running game process to recover struct layouts. That component is
**not part of this repository**, so `generate structs` is disabled in the
open-source build and will report that it is unavailable. The generated struct
model it produces is already checked in under `ClientResources/Structs`.

## Planned features

- **Repacking** : write changes back into `.bin` archives.
- **DDS texture import** : convert a `.dds` back into the game's texture
  format (the `texture dds import` command exists but is not functional yet).
- **Support for the 15.0+ bin format** (see the notice above).
- **Blender plugin integration** : import game geometry directly into Blender.
- **Godot engine plugin integration** : bring assets into Godot.
- **More asset conversion** ; additional formats such as sounds, maps, more texture
  types, and other resources.

Contributions toward any of these are very welcome.

## Contributing

Issues and pull requests are welcome. See [CONTRIBUTING.md](CONTRIBUTING.md).

## License

Released under the [MIT License](LICENSE) — you may use, modify, and
redistribute it freely, including commercially, as long as the copyright and
license notice are preserved.
