(function () {
  'use strict';

  var isExamPage = window.location.pathname.indexOf('exam.html') !== -1;

  // ── PWA / iOS meta injection (runs on every page, including exam) ──
  function initPWA() {
    if (!document.querySelector('link[rel="manifest"]')) {
      var l = document.createElement('link');
      l.rel = 'manifest';
      l.href = (window.location.pathname.indexOf('/pages/') === 0 ? '../../' : '') + 'manifest.json';
      document.head.appendChild(l);
    }
    if (!document.querySelector('meta[name="apple-mobile-web-app-capable"]')) {
      var m = document.createElement('meta');
      m.name = 'apple-mobile-web-app-capable';
      m.content = 'yes';
      document.head.appendChild(m);
    }
    if (!document.querySelector('meta[name="apple-mobile-web-app-status-bar-style"]')) {
      var s = document.createElement('meta');
      s.name = 'apple-mobile-web-app-status-bar-style';
      s.content = 'black-translucent';
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
  }

  // ── Back button (injected into navbar on every page) ──
  function initBackButton() {
    var navbar = document.querySelector('.navbar');
    if (!navbar) return;
    if (document.querySelector('.back-btn')) return;

    var btn = document.createElement('button');
    btn.className = 'back-btn';
    btn.setAttribute('aria-label', 'Go back to previous page');
    btn.setAttribute('type', 'button');
    btn.innerHTML =
      '<svg width="22" height="22" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round">' +
        '<polyline points="15 18 9 12 15 6"/>' +
      '</svg>';
    btn.addEventListener('click', function () {
      if (document.referrer || window.history.length > 1) {
        window.history.back();
      }
    });
    if (!document.referrer && window.history.length <= 1) {
      btn.style.display = 'none';
    }
    navbar.insertBefore(btn, navbar.firstChild);
  }

  // ── Hamburger menu (skipped on exam.html) ──
  var _panelCreated = false;
  var _panel, _backdrop, _btn;

  function ensurePanel() {
    if (_panelCreated) return;
    _panelCreated = true;

    var navbar = document.querySelector('.navbar');
    if (!navbar) return;

    var navbarNav = navbar.querySelector('.navbar-nav');

    _backdrop = document.createElement('div');
    _backdrop.className = 'mobile-menu-backdrop';

    _panel = document.createElement('div');
    _panel.className = 'mobile-menu';
    _panel.setAttribute('role', 'dialog');
    _panel.setAttribute('aria-modal', 'true');
    _panel.setAttribute('aria-label', 'Navigation menu');

    var closeBtn = document.createElement('button');
    closeBtn.className = 'mobile-menu-close';
    closeBtn.setAttribute('aria-label', 'Close navigation menu');
    closeBtn.setAttribute('type', 'button');
    closeBtn.innerHTML =
      '<svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">' +
        '<line x1="18" y1="6" x2="6" y2="18"/><line x1="6" y1="6" x2="18" y2="18"/>' +
      '</svg>';
    _panel.appendChild(closeBtn);

    if (navbarNav) {
      var navClone = navbarNav.cloneNode(true);
      navClone.className = 'mobile-nav-list';
      var userItem = navClone.querySelector('.navbar-user');
      if (userItem) userItem.remove();
      _panel.appendChild(navClone);
    }

    var sidebar = document.querySelector('.sidebar-nav');
    if (sidebar) {
      var sideClone = sidebar.cloneNode(true);
      sideClone.className = 'mobile-sidebar-list';
      _panel.appendChild(sideClone);
    }

    if (!navbarNav && !sidebar) {
      var prefix = window.location.pathname.indexOf('/pages/') === 0 ? '../../' : '';
      var defaultList = document.createElement('ul');
      defaultList.className = 'mobile-nav-list';
      var defaultLinks = [
        { href: prefix + 'index.html', text: 'Home' },
        { href: prefix + 'login.html', text: 'Login' },
        { href: prefix + 'register.html', text: 'Register' }
      ];
      defaultLinks.forEach(function (link) {
        var li = document.createElement('li');
        var a = document.createElement('a');
        a.href = link.href;
        a.textContent = link.text;
        li.appendChild(a);
        defaultList.appendChild(li);
      });
      _panel.appendChild(defaultList);
    }

    document.body.appendChild(_backdrop);
    document.body.appendChild(_panel);

    _backdrop.addEventListener('click', closeMenu);
    closeBtn.addEventListener('click', closeMenu);

    _panel.querySelectorAll('a').forEach(function (link) {
      link.addEventListener('click', closeMenu);
    });

    document.addEventListener('keydown', function (e) {
      if (e.key === 'Escape' && _panel.classList.contains('open')) closeMenu();
    });
  }

  function openMenu() {
    ensurePanel();
    _panel.classList.add('open');
    _backdrop.classList.add('open');
    _btn.classList.add('active');
    _btn.setAttribute('aria-expanded', 'true');
    document.body.style.overflow = 'hidden';
  }

  function closeMenu() {
    if (!_panel) return;
    _panel.classList.remove('open');
    _backdrop.classList.remove('open');
    _btn.classList.remove('active');
    _btn.setAttribute('aria-expanded', 'false');
    document.body.style.overflow = '';
  }

  function initHamburger() {
    var navbar = document.querySelector('.navbar');
    if (!navbar) return;
    if (_btn) return;

    _btn = navbar.querySelector('.hamburger-btn');
    if (!_btn) {
      _btn = document.createElement('button');
      _btn.className = 'hamburger-btn';
      navbar.appendChild(_btn);
    }
    _btn.setAttribute('aria-label', 'Open navigation menu');
    _btn.setAttribute('aria-expanded', 'false');
    _btn.setAttribute('type', 'button');
    if (!_btn.querySelector('svg')) {
      _btn.innerHTML =
        '<svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" class="hamburger-icon">' +
          '<line class="hamburger-line top" x1="3" y1="6" x2="21" y2="6"/>' +
          '<line class="hamburger-line mid" x1="3" y1="12" x2="21" y2="12"/>' +
          '<line class="hamburger-line bot" x1="3" y1="18" x2="21" y2="18"/>' +
        '</svg>';
    }

    // Attach click handler immediately — no DOMContentLoaded wait
    _btn.addEventListener('click', function (e) {
      e.stopPropagation();
      if (_panel && _panel.classList.contains('open')) closeMenu();
      else openMenu();
    });
  }

  // ── Boot ──
  initPWA();
  initBackButton();

  // Init hamburger immediately (button exists in HTML, just attach listener)
  if (!isExamPage) {
    initHamburger();
  }

  // Ensure panel is created by DOMContentLoaded (or right now if already past)
  function bootPanel() {
    if (!isExamPage && !_panelCreated) {
      ensurePanel();
    }
  }
  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', bootPanel);
  } else {
    bootPanel();
  }
})();
