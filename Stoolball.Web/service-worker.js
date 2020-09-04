addEventListener('install', function (event) {
	event.waitUntil(caches.open('core').then(function (cache) {
		cache.add(new Request('/httpstatus/offline.html'));
		cache.add(new Request('/css/base.min.css'));
		cache.add(new Request('/images/logos/stoolball-england.svg'));
		return;
	}));
});

addEventListener('fetch', function (event) {
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