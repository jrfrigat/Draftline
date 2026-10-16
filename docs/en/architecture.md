# Architecture

Draftline is a client-only Blazor WebAssembly application: no server, no API. The static build is the
whole product.

## Projects

| Project | Role |
| :-- | :-- |
| `Draftline.App` | The editor, published as a PWA |
| `Flare.Theme.OneC` | An unofficial 1C-style Flare theme (.NET 8, public Flare API), packable on its own |
| `Draftline.App.Tests` | xUnit v3 specs on Microsoft.Testing.Platform |

## Inside `Draftline.App`

| Folder | Contents |
| :-- | :-- |
| `Model` | The domain: `DraftDocument`, `DraftLayer` (a screen), `WireframeElement` (a block), variants, components, geometry, guides, element states |
| `Services` | `DocumentSession` (the open document and every command on it), `DocumentHistory`, `AutosaveService`, `ProjectCatalog`, storage adapters, `DraftlineJson`, `DocumentFileService` and `SvgExporter` |
| `Components`, `Pages` | The interface: canvas, palette, inspectors, panels. `Pages/Home.razor` is the editor page |
| `Theming` | `ThemeCoordinator` and `MockupThemeResolver`: the editor and the mockup are themed independently |
| `wwwroot` | `index.html`, the service workers, the manifest, icons, `css/draftline.css` and JS modules (`js/draftline-*.js`) |

## Data flow

1. `Home.razor` asks `ProjectCatalog` for the last project and opens it in `DocumentSession`.
2. Every edit goes through `DocumentSession`, which records it in `DocumentHistory` (one gesture, such as
   a drag, is one undo step) and raises `Changed`.
3. `AutosaveService` writes the dirty document through `ProjectCatalog` to `IBrowserStorage`, debounced,
   and flushes before switching projects or leaving the page.
4. `IBrowserStorage` is `FallbackBrowserStorage` over `IndexedDbBrowserStorage`: IndexedDB when the
   browser allows it, memory when it does not.
5. The canvas renders blocks through `WireframeElementView`; `ElementFragmentCache` reuses the fragment
   of a block that only moved, so a drag does not re-render its content.
6. Export draws every screen as SVG through `SvgExporter`. PNG and PDF are rasterized from that SVG in the
   browser by `js/draftline-export.js`, and ZIP bundles are built locally by `js/draftline-zip.js`.

## Import

`DocumentFileService` hands the chosen file to .NET as a stream (`IJSStreamReference`), so it never
becomes a JavaScript string. `DraftlineJson.ReadAsync` reads it in chunks with byte progress, checks the
header with `Utf8JsonReader`, deserializes once and normalizes the screens in batches.
`ProjectCatalog.PrepareOwnedImportAsync` adopts the parsed model without copying it, and
`CommitImportAsync` stores it atomically. The editor is inert while an import runs.

## Flare

The shell (ribbon, dockable panels, status line) is `Flare.Components.IDE`. The components and tokens come
from Flare 0.44 on nuget.org. Changes to the library itself belong in the
[Flare repository](https://github.com/jrfrigat/Flare), not here.
