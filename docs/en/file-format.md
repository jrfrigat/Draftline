# File Format

## The document

A wireframe is one JSON document (`*.wireframe.json`). It holds the project's name and identity, its
screens (`layers`) with their blocks, screen variants, reusable components, the mockup theme and the
embedded images. The serializer is source-generated `System.Text.Json`, so the format works under
WebAssembly trimming.

- **Export** writes readable, indented JSON. **Storage** inside the browser writes compact JSON.
- Only stored state is written: computed views (such as "every block of every screen") and block fields
  equal to their defaults are left out, and reading restores them. A large wireframe therefore stays a
  few megabytes rather than tens.
- Older files that still contain the computed fields are read; the fields are ignored.

## Schema versions

The current schema is **9** (`DraftDocument.CurrentSchemaVersion`). Older schemas (1-8) are migrated when
a file is read. A file from a newer Draftline is refused with a message that names both versions, rather
than opened with fields silently lost.

A damaged document is repaired on read: missing screens, broken references and invalid values are
replaced so the editor never has to defend itself against a half-written file. A file that is not a
Draftline wireframe at all is refused.

## Images

Pasted images are PNG, JPEG, GIF or WebP up to 5 MB. Larger pictures are scaled down to 2048 px and about
2 MB. Images are embedded in the document, so an exported JSON is complete on its own.

## Storage in the browser

| Key | Content |
| :-- | :-- |
| `draftline.index` | The project list: identity, name, last change, block count |
| `draftline.project.<id>` | One project's document (compact JSON) |
| `draftline.last` | The project to reopen |

The keys live in IndexedDB. An import writes the project, the list and the last-open project in one
transaction, so a failed import leaves the catalogue as it was. Entries from older versions in
`localStorage` are moved over transactionally: an original is deleted only after it was written. When
the browser refuses storage, the editor keeps working in memory and says so. A quota error keeps the
"unsaved changes" mark.

## Offline

The published app registers a service worker. It caches the whole build on install, using the SDK's
asset list (`service-worker-assets.js`), and answers cache-first. The cache is named
`draftline-<assetsManifest.version>`, so a new release replaces the previous cache. The development server
caches nothing. Roboto for the official Material theme comes from Google Fonts; offline, the browser falls
back to an available font.
