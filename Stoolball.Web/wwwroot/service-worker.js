const version = "__ServiceWorkerVersion__";
const coreCache = version + "_core";
const cacheIDs = [coreCache];

// Cache a branded offline page
self.addEventListener("install", function (event) {
  event.waitUntil(
    caches.open(coreCache).then(function (cache) {
      return cache.addAll([
        "/status/offline.html",
        "/css/base.min.css",
        "/css/status.min.css",
        "/favicon.ico",
        "/images/logos/stoolball-england-icon.svg",
        "/images/logos/stoolball-england.svg",
        "/images/photos/duck.jpg",
        "/fonts/mada-v9-latin-regular.woff2",
        "/fonts/mada-v9-latin-700.woff2",
        "/fonts/assistant-v6-latin-600.woff2",
        "/fonts/assistant-v6-latin-700.woff2",
        "/fonts/assistant-v6-latin-800.woff2",
      ]);
    })
  );
});

// On version update, remove old cached files
self.addEventListener("activate", function (event) {
  event.waitUntil(
    caches
      .keys()
      .then(function (keys) {
        return Promise.all(
          keys
            .filter(function (key) {
              return !cacheIDs.includes(key);
            })
            .map(function (key) {
              return caches.delete(key);
            })
        );
      })
      .then(function () {
        return self.clients.claim();
      })
  );
});

// Handle requests - return the offline page if they fail
self.addEventListener("fetch", function (event) {
  var request = event.request;

  // Bug fix https://stackoverflow.com/a/49719964
  if (
    event.request.cache === "only-if-cached" &&
    event.request.mode !== "same-origin"
  )
    return;

  // HTML files
  // Network-first
  if ((request.headers.get("Accept") || "").includes("text/html")) {
    event.respondWith(
      fetch(request).catch(function () {
        return caches.match("/status/offline.html");
      })
    );
    return;
  }

  // Other files, such as the CSS, images and fonts used by the offline page
  // Network-first, falling back to the cache
  if (request.method === "GET") {
    event.respondWith(
      fetch(request).catch(function (error) {
        return caches.match(request).then(function (response) {
          return response || Promise.reject(error);
        });
      })
    );
  }
});
