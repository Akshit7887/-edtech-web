(function () {
  'use strict';

  var isExamPage = window.location.pathname.indexOf('exam.html') !== -1;

  // ── PWA / iOS meta injection ──
  function initPWA() {
    try {
      if (!document.querySelector('link[rel="manifest"]')) {
        var l = document.createElement('link');
        l.rel = 'manifest';
        l.href = (window.location.pathname.indexOf('/pages/') === 0 ? '../../' : '') + 'manifest.json';
        document.head.appendChild(l);
      }
      if (!document.querySelector('meta[name="apple-mobile-web-app-capable"]')) {
        var m = document.createElement('meta');
        m.name = 'apple-mobile-web-app-capable'; m.content = 'yes';
        document.head.appendChild(m);
      }
      if (!document.querySelector('meta[name="apple-mobile-web-app-status-bar-style"]')) {
        var s = document.createElement('meta');
        s.name = 'apple-mobile-web-app-status-bar-style'; s.content = 'black-translucent';
        document.head.appendChild(s);
      }
      if (!document.querySelector('link[rel="apple-touch-icon"]')) {
        var i = document.createElement('link');
        i.rel = 'apple-touch-icon';
        i.href = (window.location.pathname.indexOf('/pages/') === 0 ? '../../' : '') + 'icons/icon-192x192.png';
        document.head.appendChild(i);
      }
      if ('serviceWorker' in navigator) {
        navigator.serviceWorker.register('/sw.js').catch(function () {});
      }
    } catch (e) { /* ignore PWA errors */ }
  }

  // ── Back button ──
  function initBackButton() {
    try {
      var navbar = document.querySelector('.navbar');
      if (!navbar) return;
      if (document.querySelector('.back-btn')) return;
      var btn = document.createElement('button');
      btn.className = 'back-btn';
      btn.setAttribute('aria-label', 'Go back to previous page');
      btn.setAttribute('type', 'button');
      btn.innerHTML = '<svg width="22" height="22" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round"><polyline points="15 18 9 12 15 6"/></svg>';
      btn.addEventListener('click', function () {
        if (document.referrer || window.history.length > 1) window.history.back();
      });
      if (!document.referrer && window.history.length <= 1) btn.style.display = 'none';
      navbar.insertBefore(btn, navbar.firstChild);
    } catch (e) { /* ignore back-btn errors */ }
  }

  // ── Hamburger menu ──
  var panel, backdrop, btn;

  function closeMenu() {
    if (!panel) return;
    panel.classList.remove('open');
    backdrop.classList.remove('open');
    if (btn) {
      btn.classList.remove('active');
      btn.setAttribute('aria-expanded', 'false');
    }
    document.body.style.overflow = '';
  }

  function openMenu() {
    if (!panel) return;
    panel.classList.add('open');
    backdrop.classList.add('open');
    btn.classList.add('active');
    btn.setAttribute('aria-expanded', 'true');
    document.body.style.overflow = 'hidden';
  }

  function toggleMenu() {
    if (!panel) return;
    if (panel.classList.contains('open')) closeMenu();
    else openMenu();
  }

  function initHamburger() {
    try {
      var navbar = document.querySelector('.navbar');
      if (!navbar) return;

      btn = navbar.querySelector('.hamburger-btn');
      if (!btn) {
        btn = document.createElement('button');
        btn.className = 'hamburger-btn';
        navbar.appendChild(btn);
      }
      btn.setAttribute('aria-label', 'Open navigation menu');
      btn.setAttribute('aria-expanded', 'false');
      btn.setAttribute('type', 'button');
      if (!btn.querySelector('svg')) {
        btn.innerHTML = '<svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" class="hamburger-icon"><line class="hamburger-line top" x1="3" y1="6" x2="21" y2="6"/><line class="hamburger-line mid" x1="3" y1="12" x2="21" y2="12"/><line class="hamburger-line bot" x1="3" y1="18" x2="21" y2="18"/></svg>';
      }

      panel = document.createElement('div');
      panel.className = 'mobile-menu';
      panel.setAttribute('role', 'dialog');
      panel.setAttribute('aria-modal', 'true');
      panel.setAttribute('aria-label', 'Navigation menu');

      var closeBtn = document.createElement('button');
      closeBtn.className = 'mobile-menu-close';
      closeBtn.setAttribute('aria-label', 'Close navigation menu');
      closeBtn.setAttribute('type', 'button');
      closeBtn.innerHTML = '<svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><line x1="18" y1="6" x2="6" y2="18"/><line x1="6" y1="6" x2="18" y2="18"/></svg>';
      panel.appendChild(closeBtn);

      var navbarNav = navbar.querySelector('.navbar-nav');
      if (navbarNav) {
        var navClone = navbarNav.cloneNode(true);
        navClone.className = 'mobile-nav-list';
        var userItem = navClone.querySelector('.navbar-user');
        if (userItem) userItem.remove();
        panel.appendChild(navClone);
      }

      var sidebar = document.querySelector('.sidebar-nav');
      if (sidebar) {
        var sideClone = sidebar.cloneNode(true);
        sideClone.className = 'mobile-sidebar-list';
        panel.appendChild(sideClone);
      }

      if (!navbarNav && !sidebar) {
        var pfx = window.location.pathname.indexOf('/pages/') === 0 ? '../../' : '';
        var ul = document.createElement('ul');
        ul.className = 'mobile-nav-list';
        [{ h: pfx + 'index.html', t: 'Home' }, { h: pfx + 'login.html', t: 'Login' }, { h: pfx + 'register.html', t: 'Register' }].forEach(function (x) {
          var li = document.createElement('li');
          var a = document.createElement('a');
          a.href = x.h; a.textContent = x.t;
          li.appendChild(a); ul.appendChild(li);
        });
        panel.appendChild(ul);
      }

      backdrop = document.createElement('div');
      backdrop.className = 'mobile-menu-backdrop';

      document.body.appendChild(backdrop);
      document.body.appendChild(panel);

      // Wire up events
      btn.onclick = toggleMenu;
      closeBtn.onclick = closeMenu;
      backdrop.onclick = closeMenu;
      document.addEventListener('keydown', function (e) {
        if (e.key === 'Escape' && panel.classList.contains('open')) closeMenu();
      });
      panel.querySelectorAll('a').forEach(function (link) {
        link.addEventListener('click', closeMenu);
      });
    } catch (e) {
      console.error('[nav] initHamburger failed:', e);
    }
  }

  initPWA();
  initBackButton();
  if (!isExamPage) {
    initHamburger();
  }
})();
