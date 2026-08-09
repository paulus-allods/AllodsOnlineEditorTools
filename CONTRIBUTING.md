# Contributing

Thanks for your interest in improving Allods Online Editor Tools! Issues and
pull requests are welcome.

## Getting started

1. Install the [.NET SDK 10.0](https://dotnet.microsoft.com/download) or later.
2. Fork and clone the repository.
3. Build and run the tests:

   ```sh
   dotnet build EditorCLI/EditorCLI.csproj
   dotnet test ClientResources.Tests/ClientResources.Tests.csproj
   ```

> **Note** : The `StructCodeGeneration` project is not part of this repository.
> The open-source build excludes it automatically, so build the individual
> project files as shown above rather than the solution.

## Pull requests

- Keep changes focused; one logical change per pull request.
- Match the style of the surrounding code. The repository targets modern C# with
  nullable reference types enabled.
- Add or update tests when you change behavior. Struct layouts are covered by
  `ClientResources.Tests`, please keep those green.
- Make sure the build and test commands from [Getting started](#getting-started)
  pass before opening the PR.
- Write a clear description of *what* changed and *why*.

## Reverse-engineered data

Much of this project describes undocumented, version-specific game data
formats. When adding or correcting struct layouts, formats, or version support:

- State which **game version** your change was verified against.
- Prefer changes you can back with a concrete sample or a test.
- Don't commit any game assets or copyrighted data.

## Using AI coding assistants

AI coding assistants (Claude, Copilot, Cursor, and similar) are welcome here.
The rules below boil down to one principle: **the human who submits the code is fully
responsible for it.**

- **You own the contribution.** Review, understand, and test every line before
  you submit it, whether you or a tool wrote it.
- **Verify licensing.** Do not submit code an assistant reproduced from an
  incompatible source. Everything you contribute must be your own work, licensed
  under this project's license.
- **Disclose the assistance.** If an AI tool did non-trivial work on a change,
  note it in the commit message with an `Assisted-by:` trailer naming the tool
  and model, for example:

  ```
  Assisted-by: Claude (claude-opus-4)
  ```

  Don't bother listing ordinary tooling (editors, compilers, formatters).
- **Keep the assistant honest about this project.** These formats are
  undocumented and version-specific; assistants will confidently invent field
  names, sizes, and offsets. Treat generated format/struct changes with extra
  suspicion and verify them against real data.

## Getting help with a specific game version

If your issue is specific to a client version this project doesn't yet handle
well, the maintainers may need a copy of that client to reproduce it. Please
upload your client to
[community.allods-developers.eu](https://community.allods-developers.eu/) and
reference it in the issue.