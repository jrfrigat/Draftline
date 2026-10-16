# Security Policy

## Supported versions

Draftline is pre-1.0; only the latest released version and the site published from `main` receive
security fixes.

| Version | Supported |
| ------- | --------- |
| latest  | yes       |
| older   | no        |

## Reporting a vulnerability

**Please do not report security vulnerabilities through public GitHub issues.**

Instead, use GitHub's private reporting:

1. Go to the repository's **Security** tab.
2. Click **Report a vulnerability** (Privately report a vulnerability).
3. Describe the issue, affected version(s), and steps to reproduce.

We aim to acknowledge a report within a few days and will keep you updated on the fix and disclosure
timeline. Please give us reasonable time to address the issue before any public disclosure.

## Hardening notes

- Draftline has no backend: documents stay in the browser's IndexedDB and leave it only as files the
  person exports.
- `index.html` sets a Content-Security-Policy that allows scripts, connections and workers from the app's
  own origin only, plus styles and fonts from Google Fonts.
- An imported JSON file is treated as untrusted data: it is size-limited, schema-checked and normalized
  before it is opened, and a file from a newer schema is refused.
- Pasted images are limited to PNG, JPEG, GIF and WebP and are re-encoded when they are too large.

Thank you for helping keep Draftline and its users safe.
