# Getting Started

## Open Draftline

- **In the browser**: <https://jrfrigat.github.io/Draftline/>. Nothing to install; the browser can also
  install it as an app (PWA), which then opens without a network.
- **Locally**: with the .NET 10 SDK installed,

  ```powershell
  dotnet run --project src/Draftline.App
  ```

  and open the HTTP address the command prints. The development server does not cache the app, so a
  refresh always shows the latest build.

The interface is in Russian.

## The first wireframe

1. A new project opens with one empty screen. The **Blocks** panel on the left lists every kind of block;
   type in its search field to find one.
2. Drag a block onto the canvas or click it to place it in free space. Select a block to edit its text,
   data and states in **Properties** on the right.
3. Add screens in the **Screens** panel. A button, a link or a menu item can open another screen or show
   it as a dialog: pick the transition in its properties.
4. The ribbon holds the commands: **Файл** (File: import and export), **Макет** (Layout: search, undo,
   redo), **Блок** (Block) and **Вид** (View: themes, preview, panels).

Every change is saved to the browser automatically, a moment after you stop editing; the status line
shows whether the save went through.

## Preview

**Вид → Просмотр** (View → Preview) shows the wireframe as a person would see it: transitions work,
checkboxes, tabs, dropdowns and tree branches change state, and nothing is edited. **К редактору** goes
back. A preview state is temporary and never changes the document.

## Files

- **Файл → Выгрузить JSON** (Export JSON) writes the whole wireframe, images included, into one portable
  file.
- **Файл → Загрузить JSON** (Import JSON) opens such a file as a new project. A file whose project already
  exists in this browser opens as a copy with a new identity, so nothing is overwritten. A large file
  shows its progress while it is read, checked, saved and opened.
- **SVG, PNG and PDF** export the current screen or every screen at once (a ZIP for SVG and PNG). A PDF
  keeps transitions as clickable links between its pages.

## Where the data lives

Projects stay in this browser's IndexedDB, and nothing leaves your machine. Clearing site data removes
them, and a private window may not keep them at all: Draftline then warns that it works in memory only.
Export JSON to keep a copy or to move a wireframe elsewhere. See [File Format](file-format.md).
