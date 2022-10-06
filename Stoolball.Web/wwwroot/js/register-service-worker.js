if (navigator && navigator.serviceWorker) {
    navigator.serviceWorker.register('/service-worker.js?v=__ServiceWorkerVersion__');
}