// Tombstone service worker. Trainfree is online-only (see CLAUDE.md), so there is no
// offline caching any more. This file exists solely to evict the Blazor template's
// offline worker from browsers that already installed it: deleting the file outright
// would leave those installs serving a stale build forever, because a controlled tab
// keeps the old worker alive until a *new* service-worker.js is fetched.
//
// skipWaiting + clients.claim are required: without them this worker would sit in
// "waiting" behind the very worker it is meant to replace.
//
// Safe to delete once no browser can plausibly still be running the offline worker.

self.addEventListener('install', () => self.skipWaiting());

self.addEventListener('activate', event => event.waitUntil(onActivate()));

async function onActivate() {
    const cacheKeys = await caches.keys();
    await Promise.all(cacheKeys
        .filter(key => key.startsWith('offline-cache-'))
        .map(key => caches.delete(key)));

    await self.registration.unregister();
    await self.clients.claim();

    // Reload controlled tabs so they leave the cached shell and hit the network.
    const clients = await self.clients.matchAll({ type: 'window' });
    for (const client of clients) {
        client.navigate(client.url);
    }
}
