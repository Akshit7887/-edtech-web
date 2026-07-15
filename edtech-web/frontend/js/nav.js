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

  // ── Hamburger menu (skipped on exam.html) ──
  function initMobileNav() {
    var navbar = document.querySelector('.navbar');
    if (!navbar) return;

    // Prevent double-init
    if (navbar.querySelector('.hamburger-btn')) return;

    var navbarNav = navbar.querySelector('.navbar-nav');

    // ── Hamburger button (use existing if present in HTML, or create one) ──
    var btn = navbar.querySelector('.hamburger-btn');
    if (!btn) {
      btn = document.createElement('button');
      btn.className = 'hamburger-btn';
      navbar.appendChild(btn);
    }
    btn.setAttribute('aria-label', 'Open navigation menu');
    btn.setAttribute('aria-expanded', 'false');
    btn.setAttribute('type', 'button');
    if (!btn.querySelector('svg')) {
      btn.innerHTML =
        '<svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" class="hamburger-icon">' +
          '<line class="hamburger-line top" x1="3" y1="6" x2="21" y2="6"/>' +
          '<line class="hamburger-line mid" x1="3" y1="12" x2="21" y2="12"/>' +
          '<line class="hamburger-line bot" x1="3" y1="18" x2="21" y2="18"/>' +
        '</svg>';
    }

    // ── Backdrop ──
    var backdrop = document.createElement('div');
    backdrop.className = 'mobile-menu-backdrop';

    // ── Panel ──
    var panel = document.createElement('div');
    panel.className = 'mobile-menu';
    panel.setAttribute('role', 'dialog');
    panel.setAttribute('aria-modal', 'true');
    panel.setAttribute('aria-label', 'Navigation menu');

    // Close button
    var closeBtn = document.createElement('button');
    closeBtn.className = 'mobile-menu-close';
    closeBtn.setAttribute('aria-label', 'Close navigation menu');
    closeBtn.setAttribute('type', 'button');
    closeBtn.innerHTML =
      '<svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">' +
        '<line x1="18" y1="6" x2="6" y2="18"/><line x1="6" y1="6" x2="18" y2="18"/>' +
      '</svg>';
    panel.appendChild(closeBtn);

    // Clone navbar-nav links (after setupNavbar may have run)
    if (navbarNav) {
      var navClone = navbarNav.cloneNode(true);
      navClone.className = 'mobile-nav-list';
      var userItem = navClone.querySelector('.navbar-user');
      if (userItem) userItem.remove();
      panel.appendChild(navClone);
    }

    // Clone sidebar-nav if present (also goes into the panel)
    var sidebar = document.querySelector('.sidebar-nav');
    if (sidebar) {
      var sideClone = sidebar.cloneNode(true);
      sideClone.className = 'mobile-sidebar-list';
      panel.appendChild(sideClone);
    }

    // If no navbarNav and no sidebar, add default navigation links
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
      panel.appendChild(defaultList);
    }

    document.body.appendChild(backdrop);
    document.body.appendChild(panel);

    // ── Toggle logic ──
    function openMenu() {
      panel.classList.add('open');
      backdrop.classList.add('open');
      btn.classList.add('active');
      btn.setAttribute('aria-expanded', 'true');
      document.body.style.overflow = 'hidden';
    }

    function closeMenu() {
      panel.classList.remove('open');
      backdrop.classList.remove('open');
      btn.classList.remove('active');
      btn.setAttribute('aria-expanded', 'false');
      document.body.style.overflow = '';
    }

    btn.addEventListener('click', function (e) {
      e.stopPropagation();
      if (panel.classList.contains('open')) closeMenu();
      else openMenu();
    });

    backdrop.addEventListener('click', closeMenu);
    closeBtn.addEventListener('click', closeMenu);

    // Close on any link tap
    panel.querySelectorAll('a').forEach(function (link) {
      link.addEventListener('click', closeMenu);
    });

    // Close on Escape
    document.addEventListener('keydown', function (e) {
      if (e.key === 'Escape' && panel.classList.contains('open')) closeMenu();
    });

    // Handle window resize: close panel on orientation change
    // (no longer auto-closes above 768px since hamburger is now universal)
  }

  // ── Boot ──
  initPWA();

  if (!isExamPage) {
    if (document.readyState === 'loading') {
      document.addEventListener('DOMContentLoaded', initMobileNav);
    } else {
      initMobileNav();
    }
  }
})();
