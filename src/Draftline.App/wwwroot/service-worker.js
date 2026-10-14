// The development worker. It exists so the browser registers the same path the published worker
// takes, and it deliberately caches nothing: a cache while you are editing an app is a stale app you
// cannot explain. The offline shell lives in service-worker.published.js.
self.addEventListener('fetch', () => { });
