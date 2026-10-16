# Deployment

Draftline is a set of static files. Any host that serves them works; there is nothing to configure at
runtime.

## GitHub Pages

`.github/workflows/pages.yml` publishes every push to `main` to `https://<owner>.github.io/<repo>/`.

One-time setup: **Settings → Pages → Build and deployment → Source: GitHub Actions**.

The workflow:

1. Rewrites `<base href="/">` in `src/Draftline.App/wwwroot/index.html` to `/<repo>/` **before**
   publishing. The service worker's asset list carries a hash of `index.html`, so the file has to be
   rewritten before that list is generated. The repository keeps `/` for `dotnet run` and root hosting.
2. Runs `dotnet publish -c Release`.
3. Copies `index.html` to `404.html`, so a deep link opens the app instead of a Pages error, and adds
   `.nojekyll`.
4. Uploads `dist/wwwroot` and deploys it with `actions/deploy-pages`.

To check a Pages build locally, publish with the rewritten base and serve the output under the same
sub-path, for example `http://localhost:8080/Draftline/`.

## Any static host

```powershell
dotnet publish src/Draftline.App -c Release -o dist
```

Serve `dist/wwwroot` from the site root. For another sub-path, change `<base href>` in `index.html`
before publishing, as the Pages workflow does. The host should:

- serve `.wasm` as `application/wasm` and `.webmanifest` as `application/manifest+json`;
- serve the pre-compressed `.br`/`.gz` files when the browser accepts them (optional, but faster);
- fall back to `index.html` (or `404.html`) for unknown paths;
- be HTTPS, since a service worker needs it outside `localhost`.

`index.html` carries a Content-Security-Policy that allows the app's own origin and Google Fonts only.

## Release

1. Make sure `main` is green (CI).
2. Tag it: `git tag v0.4.0 && git push origin v0.4.0`.
3. The `Release` workflow builds and tests the solution, publishes the static site and creates a GitHub
   Release with `draftline-v0.4.0.zip` attached and generated notes.

Update [CHANGELOG.md](../../CHANGELOG.md) and [CHANGELOG.ru.md](../../CHANGELOG.ru.md) before tagging.
