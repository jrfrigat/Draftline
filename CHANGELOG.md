# Changelog

> [Русский](CHANGELOG.ru.md)

All notable changes to this project are documented here. The format is based on
[Keep a Changelog](https://keepachangelog.com/), and the project aims to follow
[Semantic Versioning](https://semver.org/).

## [Unreleased]

### Added

- **Publishing**: GitHub Actions for build and tests, GitHub Pages and tagged releases with a static-site
  zip; English and Russian documentation in `docs/`, MIT license, contribution and security guides.
- **Blocks**: common wireframe controls with palette search, annotations, shapes and editable charts,
  device frames and media, tables with sorting and row styles, nested tree rows, accordion and tab
  content, list and menu item states, mixed checkboxes, real calendar dates and slider ranges.
- **Editing**: nested groups, a block clipboard across screens and projects, locking and hidden-block
  recovery, alignment guides with spacing and rotation, search across screens, independent screen
  variants, reusable components whose instances keep their changes.
- **Images and icons**: embedded and cropped local images, offline icons for buttons and text.
- **Export**: PNG and linked PDF, ZIP bundles of every screen.
- **Themes**: independent Flare themes for the editor and the mockup, and the standalone
  `Flare.Theme.OneC` package.
- **Canvas**: zoom, a hand tool for small screens, keyboard access, new blocks placed in free space.

### Changed

- Projects are stored in IndexedDB; older `localStorage` entries are migrated safely.
- A JSON import streams the file and shows its progress; the stored document no longer repeats computed
  data, so large wireframes take a few megabytes instead of tens and import without running out of
  memory.
- An imported project that already exists opens as a separate copy.
- Pending edits are saved before switching projects or leaving the page.
- Flare packages are restored from nuget.org only.

### Fixed

- Damaged documents are recovered without breaking the editor.
- The published offline worker caches the whole build.
- Canvas history is protected from commands issued during a drag.

## [0.3.0] - 2026-10-14

### Added

- The editor shell on `Flare.Components.IDE`: a ribbon, dockable side panels and a status line.
- The canvas fills the window; screens are a separate panel on the right, under the properties.

## [0.2.0] - 2026-10-14

### Added

- The first versioned editor: blocks, screens and transitions, preview, JSON and SVG files, local
  storage and the offline PWA.

[Unreleased]: https://github.com/jrfrigat/Draftline/compare/v0.3.0...HEAD
[0.3.0]: https://github.com/jrfrigat/Draftline/compare/v0.2.0...v0.3.0
[0.2.0]: https://github.com/jrfrigat/Draftline/releases/tag/v0.2.0
