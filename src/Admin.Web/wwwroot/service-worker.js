const CACHE_NAME = 'my-cache-v1';

// Extracts base URL (without query parameters) for caching
function getBaseUrl(url) {
    const parsedUrl = new URL(url);
    return `${parsedUrl.origin}${parsedUrl.pathname}`;
}

self.addEventListener('install', event => {
    self.skipWaiting();
});

self.addEventListener('activate', event => {
    event.waitUntil(
        caches.keys().then(cacheNames => {
            return Promise.all(
                cacheNames.map(cacheName => {
                    if (cacheName !== CACHE_NAME) {
                        return caches.delete(cacheName);
                    }
                })
            );
        }).then(() => {
            return self.clients.claim();
        })
    );
});

self.addEventListener('fetch', event => {
    const url = new URL(event.request.url);
    const isJPEG = url.pathname.endsWith('.jpg') || url.pathname.endsWith('.jpeg');
    const baseUrl = getBaseUrl(event.request.url);

    if (isJPEG) {

        event.respondWith(
            caches.match(baseUrl).then(cachedResponse => {
                if (cachedResponse) {
                    return cachedResponse;
                }

                return fetch(event.request).then(networkResponse => {

                    return caches.open(CACHE_NAME).then(cache => {
                        cache.put(baseUrl, networkResponse.clone());
                        return networkResponse;
                    }).catch(error => {
                        throw error;
                    });
                }).catch(error => {
                    throw error;
                });
            }).catch(error => {
                throw error;
            })
        );
    }
});