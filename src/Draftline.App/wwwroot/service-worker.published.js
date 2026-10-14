// The offline shell Draftline promises: the app opens, and keeps working, with no network at all.
//
// Strategy - cache first over the app's own origin. The install puts the shell in the cache, a fetch
// is answered from the cache when it can be, and only a miss goes to the network (and is then kept,
// which is how the Blazor runtime and the Flare stylesheets get cached on the first visit). When the
// network is gone, a miss falls back to index.html so a deep link still opens the editor.
//
// CACHE_VERSION is the release of the shell. Bump it on a release that changes the app: the old
// cache is deleted on activate, and the new one refills from the network.
const CACHE_VERSION = 'draftline-v1';

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
