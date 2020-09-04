const version = '1.0.0';
const coreCache = version + "_core";
const cacheIDs = [coreCache];

// Cache a branded offline page
self.addEventListener('install', function (event) {
	event.waitUntil(caches.open(coreCache).then(function (cache) {
		cache.add(new Request('/httpstatus/offline.html'));
		cache.add(new Request('/css/base.min.css'));
		cache.add(new Request('/favicon.ico'));
		cache.add(new Request('/images/logos/stoolball-england.svg'));
		return;
	}));
});

// On version update, remove old cached files
self.addEventListener('activate', function (event) {
	event.waitUntil(caches.keys().then(function (keys) {
		return Promise.all(keys.filter(function (key) {
			return !cacheIDs.includes(key);
		}).map(function (key) {
			return caches.delete(key);
		}));
	}).then(function () {
		return self.clients.claim();
	}))
});

// Handle requests - return the offline page if they fail
self.addEventListener('fetch', function (event) {
	var request = event.request;

	// Bug fix https://stackoverflow.com/a/49719964
	if (event.request.cache === 'only-if-cached' && event.request.mode !== 'same-origin') return;

	// HTML files
	// Network-first
	if (request.headers.get('Accept').includes('text/html')) {
		event.respondWith(
			fetch(request).then(function (response) {
				return response;
			}).catch(function (error) {
				return caches.match('/httpstatus/offline.html');
			})
		)
	}
});