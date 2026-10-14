// The offline shell Draftline promises: the app opens, and keeps working, with no network at all.
//
// Strategy - cache first over the app's own origin. The install fills the cache with the whole build
// (the list comes from service-worker-assets.js, which the SDK writes next to this file and rewrites
// whenever the build changes), a fetch is answered from the cache when it can be, and only a miss
// goes to the network. When the network is gone, a miss for a page falls back to index.html so a deep
// link still opens the editor.
//
// Reading the build's own list is what makes the *first* offline visit work: cache-on-demand alone
// would only ever hold what the visitor happened to open before the network disappeared.

// The build's file list, with the hash of every file. It exists in a published app and not in a
// development one, so a missing file must cost the pre-cache, not the worker.
try {
    self.importScripts('service-worker-assets.js');
} catch (error) {
    self.assetsManifest = null;
}

// The manifest's version changes whenever the build does, which is what makes a new release drop the
// previous cache on activate. Without a manifest the worker falls back to one fixed name.
const MANIFEST_VERSION = (self.assetsManifest && self.assetsManifest.version) || 'v1';
const CACHE_VERSION = 'draftline-' + MANIFEST_VERSION;

// What the app needs even when the build's own list is unavailable: the document, the stylesheet the
// splash is drawn with, the manifest and the icon.
const SHELL = ['./', './index.html', './manifest.webmanifest', './draftline.svg', './css/draftline.css'];

// Everything the build contains, minus what the browser never asks for: the pre-compressed variants
// (the host serves them for the uncompressed URL) and the debugging files.
function urlsToCache() {
    const urls = new Set(SHELL);
    const assets = self.assetsManifest && self.assetsManifest.assets;

    if (assets) {
        for (const asset of assets) {
            const url = asset && asset.url;
            if (!url || url.endsWith('.br') || url.endsWith('.gz') || url.endsWith('.map') || url.endsWith('.pdb')) {
                continue;
            }
            urls.add('./' + url.replace(/^\/+/, ''));
        }
    }

    return Array.from(urls);
}

self.addEventListener('install', event => {
    event.waitUntil((async () => {
        const cache = await caches.open(CACHE_VERSION);

        // One failing file must not throw the whole install away: a build can legitimately name a file
        // the host does not serve, and an app that never installs is worse than one that installs
        // with a hole in it. cache: 'reload' takes the file from the network rather than from the
        // browser's own HTTP cache, which is what makes a new release a new release.
        await Promise.all(urlsToCache().map(async url => {
            try {
                await cache.add(new Request(url, { cache: 'reload' }));
            } catch (error) {
                // Left to the on-demand path below: the first fetch of it fills the cache instead.
            }
        }));

        await self.skipWaiting();
    })());
});

self.addEventListener('activate', event => {
    event.waitUntil((async () => {
        const names = await caches.keys();
        await Promise.all(names.filter(name => name !== CACHE_VERSION).map(name => caches.delete(name)));
        await self.clients.claim();
    })());
});

self.addEventListener('fetch', event => {
    const request = event.request;

    // Writes and cross-origin requests (the theme's font files, for one) are none of this worker's
    // business: it answers with the app's own static assets and nothing else.
    if (request.method !== 'GET') {
        return;
    }

    const url = new URL(request.url);
    if (url.origin !== self.location.origin) {
        return;
    }

    event.respondWith((async () => {
        const cached = await caches.match(request);
        if (cached) {
            return cached;
        }

        try {
            const response = await fetch(request);
            if (response && response.ok && response.type === 'basic') {
                const cache = await caches.open(CACHE_VERSION);
                cache.put(request, response.clone());
            }
            return response;
        } catch (error) {
            // Offline and not in the cache: the editor itself is the only useful answer, and only for
            // a navigation - a missing script has no index.html to fall back to.
            if (request.mode === 'navigate') {
                const shell = await caches.match('./index.html');
                if (shell) {
                    return shell;
                }
            }
            throw error;
        }
    })());
});


// The smallest set that makes the app open: the document, the splash's own stylesheet, the manifest
// and the icon. Everything else - the .NET runtime, Flare, the app's own assemblies - is cached the
// first time it is fetched.
const SHELL = ['./', './index.html', './manifest.webmanifest', './draftline.svg', './css/draftline.css'];

self.addEventListener('install', event => {
    event.waitUntil((async () => {
        const cache = await caches.open(CACHE_VERSION);
        await cache.addAll(SHELL);
        await self.skipWaiting();
    })());
});

self.addEventListener('activate', event => {
    event.waitUntil((async () => {
        const names = await caches.keys();
        await Promise.all(names.filter(name => name !== CACHE_VERSION).map(name => caches.delete(name)));
        await self.clients.claim();
    })());
});

self.addEventListener('fetch', event => {
    const request = event.request;

    // Writes and cross-origin requests (the theme's font files, for one) are none of this worker's
    // business: it answers with the app's own static assets and nothing else.
    if (request.method !== 'GET') {
        return;
    }

    const url = new URL(request.url);
    if (url.origin !== self.location.origin) {
        return;
    }

    event.respondWith((async () => {
        const cached = await caches.match(request);
        if (cached) {
            return cached;
        }

        try {
            const response = await fetch(request);
            if (response && response.ok && response.type === 'basic') {
                const cache = await caches.open(CACHE_VERSION);
                cache.put(request, response.clone());
            }
            return response;
        } catch (error) {
            // Offline and not in the cache: the editor itself is the only useful answer.
            const shell = await caches.match('./index.html');
            if (shell) {
                return shell;
            }
            throw error;
        }
    })());
});
