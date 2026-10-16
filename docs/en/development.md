# Development

## Requirements

- .NET SDK **10.0.400** or a later 10.0 feature band (see `global.json`).
- Node.js is only needed for the JavaScript syntax check; the build does not use npm.

## Build and test

```powershell
dotnet restore Draftline.slnx
dotnet build Draftline.slnx -c Release
dotnet test --solution Draftline.slnx -c Release
```

The build must be warning-free (CI builds with `-warnaserror`) and every test must pass. The tests use
xUnit v3 on Microsoft.Testing.Platform; `global.json` selects that runner for `dotnet test`.

The editor's JavaScript is plain modules that nothing compiles, so check them before a commit:

```powershell
Get-ChildItem src/Draftline.App/wwwroot -Recurse -Filter *.js | ForEach-Object {
    node --check $_.FullName
    if ($LASTEXITCODE -ne 0) { throw 'JavaScript syntax error' }
}
```

## Flare packages

Flare comes from nuget.org; `nuget.config` names only that source, so CI and a fresh clone restore the
same way. To try packages built in a local Flare checkout, add its `artifacts` folder as a user-level
source rather than editing `nuget.config`:

```powershell
dotnet nuget add source <path-to-Flare>/artifacts -n flare-artifacts
```

All Flare packages are pinned to the same version: two of them drifting apart is a broken theme.

## Solution layout

`Draftline.slnx` unites `src/Draftline.App`, `src/Flare.Theme.OneC` and `tests/Draftline.App.Tests`. See
[Architecture](architecture.md) for the folders inside the app.

## Conventions

- **Commands** on the document go through `DocumentSession`, so they reach the history and autosave. A
  component never edits the model behind the session's back.
- **The format** changes only together with `CurrentSchemaVersion` and a migration of older files, and a
  test that reads an older file.
- **Components and tokens** come from Flare. The project's own CSS (`css/draftline.css`) and JS stay
  minimal.
- **Interface text** is Russian. **Comments** explain why, not what the code already says.
- **Tests** cover behaviour: history, schema migrations, damaged JSON, geometry, export, catalogue,
  import, autosave, gestures and variants.

## Commits and branches

`main` is protected by CI. Changes arrive through a pull request with a warning-free build, green tests
and a clear description. See [CONTRIBUTING.md](../../CONTRIBUTING.md).
