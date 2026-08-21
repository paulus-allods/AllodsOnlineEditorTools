# AGENTS.md

.NET tooling that reads the client resources of the MMO Allods Online (binary
`Bin` databases, textures) and converts them to editable `jdb` (JSON) and `xdb`
(XML).

## Build, test, format

Requires the .NET SDK 10.0 or later. **Build the individual project files, not
the solution** : the `.slnx` references `StructCodeGeneration`, which is not part
of this repository, so a clean-checkout solution build fails.

```sh
dotnet build EditorCLI/EditorCLI.csproj
dotnet test ClientResources.Tests/ClientResources.Tests.csproj
dotnet format ClientResources/ClientResources.csproj --exclude ClientResources/Structs/
dotnet format EditorCLI/EditorCLI.csproj
dotnet format ClientResources.Tests/ClientResources.Tests.csproj
```

CI runs all of these and the formatting job gates merges. Make them pass before
proposing a change.

## Gotchas

- **Struct layouts are reverse-engineered and version-specific.** Files under
  `ClientResources/Structs` describe undocumented, per-version binary layouts.
  **NEVER** invent or guess a field name, size, or offset: a confident guess
  here silently corrupts data. Verify against real game data and state which
  client version you verified against. Supported versions are declared in
  `ClientResources/Structs/GameVersion.cs`.
- **`.bin` is not `.pak`.** `.bin` (sometimes "pack.bin") is the database
  format; `.pak` is an archive that may *contain* bin databases.
- **`StructCodeGeneration` is not in this repository.** The open-source build
  (`IsOpenSourceBuild=true`, the csproj default unless a local, gitignored
  `Directory.Build.props` overrides it) omits it and `#if`-disables the
  `structs generate` command. When editing `GenerateStructCodeCommand.cs`, both
  the `IS_OPEN_SOURCE_BUILD` and non-OSS branches must compile.
- **Never commit game assets** or copyrighted data. This repo ships none.

## Writing style

**NEVER use an em dash (`—`) in code comments, XML doc comments, or
documentation.** Use a comma, a colon, or parentheses instead.

**Only keep a comment if it explains something that cannot be understood from
the code itself** (e.g. a non-obvious reason, a hidden constraint, a
workaround). Comments that merely restate what the code does must be removed.

## Pull requests

- One logical change per PR; add or update tests when you change behavior, and
  keep the struct-layout tests in `ClientResources.Tests` green.
- If you did non-trivial work on a change, the human submitter notes it with an
  `Assisted-by: Claude (<model-id>)` trailer in the commit message.
