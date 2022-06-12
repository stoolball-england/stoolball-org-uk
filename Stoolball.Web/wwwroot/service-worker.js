const version = "1.0.0";
const coreCache = version + "_core";
const cacheIDs = [coreCache];

// Cache a branded offline page
self.addEventListener("install", function (event) {
  event.waitUntil(
    caches.open(coreCache).then(function (cache) {
      cache.add(new Request("/status/offline.html"));
      cache.add(new Request("/css/base.min.css"));
      cache.add(new Request("/css/status.min.css"));
      cache.add(new Request("/favicon.ico"));
      cache.add(new Request("/images/logos/stoolball-england-icon.svg"));
      cache.add(new Request("/images/logos/stoolball-england.svg"));
      cache.add(new Request("/fonts/mada-v9-latin-regular.woff2"));
      cache.add(new Request("/fonts/mada-v9-latin-700.woff2"));
      cache.add(new Request("/fonts/assistant-v6-latin-600.woff2"));
      cache.add(new Request("/fonts/assistant-v6-latin-700.woff2"));
      cache.add(new Request("/fonts/assistant-v6-latin-800.woff2"));
      return;
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
  if (request.headers.get("Accept").includes("text/html")) {
    event.respondWith(
      fetch(request)
        .then(function (response) {
          return response;
        })
        .catch(function (error) {
          return caches.match("/status/offline.html");
        })
    );
  }
});
