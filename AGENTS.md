# AGENTS.md

Operational instructions for AI coding agents working in this repository. Also
read [CONTRIBUTING.md](CONTRIBUTING.md): its "Using AI coding assistants"
section defines the rules that apply to any contribution you help produce.

## Project overview

.NET tooling for reading, converting, and inspecting the client resources of the
MMO Allods Online. It parses the game's binary `Bin` databases and textures and
converts them to editable formats (`jdb` = JSON, `xdb` = XML).

- `ClientResources` : core library: struct model, data types, geometry,
  textures, serializers (Bin / Jdb / Xdb).
- `EditorCLI` : Spectre.Console command-line front end.
- `ClientResources.Tests` : NUnit tests, including struct-layout validation.

## Build & test

Requires the .NET SDK 10.0 or later.

**Build the individual project files, not the solution.** The `.slnx` references
`StructCodeGeneration`, which is not part of this repository; building the
solution in a clean checkout will fail.

```sh
dotnet build EditorCLI/EditorCLI.csproj
dotnet test ClientResources.Tests/ClientResources.Tests.csproj
```

Always make sure both pass before proposing a change. CI runs the same commands.

## Repository-specific rules and gotchas

- **`StructCodeGeneration` is excluded.** The open-source build
  (`IsOpenSourceBuild=true`, the default) omits it, and the `structs generate`
  command is `#if`-disabled in that build. Do not assume that project or its
  types exist. When editing `GenerateStructCodeCommand.cs`, both the
  `IS_OPEN_SOURCE_BUILD` and non-OSS branches must compile.
- **Struct layouts are reverse-engineered and version-specific.** Files under
  `ClientResources/Structs` describe undocumented, per-version binary layouts.
  Do **not** invent or guess field names, sizes, or offsets — these formats are
  exactly where a confident guess produces silent data corruption. Any change
  must be verified against real game data, and state which client version it was
  verified against.
- **The `Bin` database format changed in client 15.0** and is not yet supported.
  The `.pak` archive format itself is unchanged. Don't conflate the two.
- **Never commit game assets** or copyrighted data. This repo ships none.
- Supported client versions are declared in
  `ClientResources/GameVersions.resx`.

## Conventions

- Modern C# with nullable reference types enabled; match the style of
  surrounding code.
- Add or update tests when you change behavior; keep the struct-layout tests in
  `ClientResources.Tests` green.
- Keep changes focused : one logical change per pull request.

## Attribution

If you do non-trivial work on a change, the human submitter should note it with
an `Assisted-by:` trailer in the commit message (see CONTRIBUTING.md).