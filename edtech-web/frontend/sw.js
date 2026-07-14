/* global self, caches, fetch, URL */

var CACHE_NAME = 'edtech-v1';

var CACHE_FIRST = [
  /\/css\//,
  /\/js\/(?!signalr-client)/,
  /\/icons\//,
  /\/manifest\.json$/,
  /\.(woff2?|ttf|otf)$/
];

var NETWORK_ONLY = [
  /\/api\//,
  /signalr/,
  /\/pages\/student\/exam\.html/
];

var NETWORK_FIRST = [
  /\/pages\//,
  /\/index\.html$/,
  /\/login\.html$/,
  /\/register\.html$/,
  /\/exam-list\.html$/
];

self.addEventListener('install', function (event) {
  event.waitUntil(
    caches.open(CACHE_NAME).then(function (cache) {
      return cache.addAll([
        '/',
        '/offline.html'
      ]).catch(function () {});
    })
  );
  self.skipWaiting();
});

self.addEventListener('activate', function (event) {
  event.waitUntil(
    caches.keys().then(function (keys) {
      return Promise.all(
        keys.filter(function (k) { return k !== CACHE_NAME; })
          .map(function (k) { return caches.delete(k); })
      );
    })
  );
  self.clients.claim();
});

function shouldCacheFirst(url) {
  return CACHE_FIRST.some(function (r) { return r.test(url); });
}

function shouldNetworkOnly(url) {
  return NETWORK_ONLY.some(function (r) { return r.test(url); });
}

function shouldNetworkFirst(url) {
  return NETWORK_FIRST.some(function (r) { return r.test(url); });
}

function isHTML(url) {
  return url.indexOf('.html') !== -1 || url === '/';
}

self.addEventListener('fetch', function (event) {
  var url = event.request.url;
  var path = new URL(url).pathname;

  // Network-only for API, SignalR, exam page
  if (shouldNetworkOnly(path)) {
    return;
  }

  // Cache-first for static assets
  if (shouldCacheFirst(path)) {
    event.respondWith(
      caches.match(event.request).then(function (cached) {
        return cached || fetch(event.request).then(function (res) {
          return caches.open(CACHE_NAME).then(function (cache) {
            cache.put(event.request, res.clone());
            return res;
          });
        });
      })
    );
    return;
  }

  // Network-first for pages (dashboards, login, etc.)
  if (shouldNetworkFirst(path) || isHTML(path)) {
    event.respondWith(
      fetch(event.request).then(function (res) {
        return caches.open(CACHE_NAME).then(function (cache) {
          cache.put(event.request, res.clone());
          return res;
        });
      }).catch(function () {
        return caches.match(event.request).then(function (cached) {
          if (cached) return cached;
          // For exam pages, show a specific message
          if (path.indexOf('/pages/student/exam') !== -1) {
            return new Response(
              '<!DOCTYPE html><html><head><meta charset="UTF-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>Connection Lost</title><style>body{font-family:-apple-system,BlinkMacSystemFont,sans-serif;display:flex;align-items:center;justify-content:center;min-height:100vh;margin:0;padding:20px;background:#fef2f2;color:#991b1b;text-align:center}div{max-width:400px}h1{font-size:1.5rem;margin-bottom:12px}p{font-size:0.95rem;line-height:1.5;color:#52525b}.strong{font-weight:700;color:#dc2626}</style></head><body><div><h1>&#x26A0; Connection Lost</h1><p class="strong">Do not proceed — contact your instructor immediately.</p><p>Your exam session has been interrupted. Please wait for further instructions before continuing.</p></div></body></html>',
              { status: 200, headers: { 'Content-Type': 'text/html; charset=utf-8' } }
            );
          }
          return caches.match('/offline.html');
        });
      })
    );
    return;
  }

  // Default: network-first for everything else
  event.respondWith(
    fetch(event.request).catch(function () {
      return caches.match(event.request);
    })
  );
});
