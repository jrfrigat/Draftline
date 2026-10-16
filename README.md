<p align="center"><b>English</b> - <a href="README.ru.md">Русский</a></p>

<p align="center">
  <a href="https://github.com/jrfrigat/Draftline/actions/workflows/ci.yml"><img src="https://github.com/jrfrigat/Draftline/actions/workflows/ci.yml/badge.svg" alt="CI"></a>
  <a href="https://github.com/jrfrigat/Draftline/actions/workflows/pages.yml"><img src="https://github.com/jrfrigat/Draftline/actions/workflows/pages.yml/badge.svg" alt="Pages"></a>
  <a href="LICENSE"><img src="https://img.shields.io/badge/License-MIT-blue.svg" alt="License: MIT"></a>
  <img src="https://img.shields.io/badge/.NET-10-512BD4.svg" alt=".NET 10">
</p>

# Draftline

**Draftline** is a standalone low-fi wireframe editor built with Blazor WebAssembly (.NET 10) and
[Flare](https://github.com/jrfrigat/Flare). You sketch a screen out of schematic blocks without visual
noise, link screens with transitions and click through the result. It runs entirely in the browser: no
backend, no account, and the documents stay on your machine.

**Demo:** <https://jrfrigat.github.io/Draftline/>. It is an installable PWA that keeps working offline
after the first visit.

## Features

- **Blocks**: 57 kinds of block (forms, tables, trees, tabs, menus, charts, device frames, media, notes
  and shapes) with editable text, data and states.
- **Screens**: several screens per wireframe, transitions and dialogs, independent screen variants, and an
  interactive preview with temporary states.
- **Editing**: groups, a block clipboard across screens and projects, locking, hiding, alignment guides,
  spacing and rotation, undo history and search across the whole wireframe.
- **Reuse**: components with instances that keep their own changes; local images and offline icons.
- **Themes**: the editor and the mockup are themed independently. Fluent UI 2, Material Design 3
  Expressive, the unofficial 1C-style `Flare.Theme.OneC` and a sketchy draft look are available.
- **Canvas**: zoom, fit to width, a hand tool, and keyboard selection and movement.
- **Files**: portable JSON, SVG, PNG and PDF (raster pages with clickable transitions), and ZIP bundles
  for every screen at once.
- **Large documents**: a JSON import streams the file with visible progress, so a wireframe of hundreds
  of screens opens without running out of memory.

## Quick start

### In the browser

Open <https://jrfrigat.github.io/Draftline/>. Projects are stored in the browser's IndexedDB. Export JSON
to move a wireframe to another browser or machine.

### Locally

```powershell
dotnet run --project src/Draftline.App
```

Open the HTTP address printed by the command. See [Getting started](docs/en/getting-started.md) for more.

## Install from a release

Every release (a pushed `v*` tag) attaches `draftline-vX.Y.Z.zip`: the static site, ready for any web
server that serves it from a site root. See [Deployment](docs/en/deployment.md).

## Solution layout

| Project | Purpose |
| :-- | :-- |
| `src/Draftline.App` | The editor: domain model, document session, storage, export and interface |
| `src/Flare.Theme.OneC` | An unofficial 1C-style theme for Flare, packaged on its own |
| `tests/Draftline.App.Tests` | xUnit v3 regression specs for the model, storage, import and export |

## Documentation

Full documentation lives in **[docs/](docs/README.md)** (English and Russian):

| Doc | Description |
| :-- | :-- |
| [Getting Started](docs/en/getting-started.md) | Running Draftline, the first wireframe, preview and files |
| [File Format](docs/en/file-format.md) | The JSON document, schema versions, storage and offline mode |
| [Architecture](docs/en/architecture.md) | Projects, layers, the document session and data flow |
| [Deployment](docs/en/deployment.md) | GitHub Pages, static hosting, releases |
| [Development](docs/en/development.md) | Build, tests, conventions |

## Build and test

```sh
dotnet build Draftline.slnx -c Release
dotnet test --solution Draftline.slnx -c Release
```

Requires **.NET 10** (SDK 10.0.400 or later, see `global.json`).

## Contributing and changes

- [CONTRIBUTING.md](CONTRIBUTING.md): how to contribute.
- [CHANGELOG.md](CHANGELOG.md): release notes.
- [SECURITY.md](SECURITY.md): how to report a vulnerability.

The interface is built on [Flare](https://github.com/jrfrigat/Flare).

## License

[MIT](LICENSE) © 2026 FrigaT
