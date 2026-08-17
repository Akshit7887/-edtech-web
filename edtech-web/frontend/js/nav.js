(function () {
  'use strict';

  var isExamPage = /(^|\/)exam\.html$/.test(window.location.pathname);

  // ── PWA / iOS meta injection ──
  function initPWA() {
    try {
      if (!document.querySelector('link[rel="manifest"]')) {
        var l = document.createElement('link');
        l.rel = 'manifest';
        l.href = (window.location.pathname.indexOf('/pages/') === 0 ? '../../' : '') + 'manifest.json';
        document.head.appendChild(l);
      }
      if (!document.querySelector('meta[name="theme-color"]')) {
        var theme = document.createElement('meta');
        theme.name = 'theme-color'; theme.content = '#be123c';
        document.head.appendChild(theme);
      }
      if (!document.querySelector('meta[name="mobile-web-app-capable"]')) {
        var mw = document.createElement('meta');
        mw.name = 'mobile-web-app-capable'; mw.content = 'yes';
        document.head.appendChild(mw);
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

  // ── Page Flip Transitions ──
  function initPageFlip() {
    var isInternal = document.referrer && document.referrer.indexOf(window.location.host) !== -1;
    if (isInternal) {
      if (!document.documentElement.classList.contains('page-flip-in')) {
        document.documentElement.classList.add('page-flip-in');
        document.body.classList.add('page-flip-in');
      }
      setTimeout(function () {
        document.documentElement.classList.remove('page-flip-in');
        document.body.classList.remove('page-flip-in');
      }, 500);
    }

    if (typeof window.goTo === 'function') {
      var orig = window.goTo;
      var navigating = false;
      window.goTo = function (path) {
        if (navigating) return;
        navigating = true;
        document.documentElement.classList.remove('page-flip-in');
        document.body.classList.remove('page-flip-in');
        document.documentElement.classList.add('page-flip-out');
        document.body.classList.add('page-flip-out');
        setTimeout(function () {
          orig(path);
          navigating = false;
        }, 300);
      };
    }
  }

  // ── Dynamic Logo Icon ──
  function initLogoAnim() {
    var logo = document.querySelector('.brand-logo');
    if (!logo) return;
    setInterval(function () {
      logo.classList.add('shine');
      setTimeout(function () { logo.classList.remove('shine'); }, 2000);
    }, 6000);
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

  // ── SVG Icons ──
  function icon(name) {
    var map = {
      home:        '<svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M3 9l9-7 9 7v11a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2z"/><polyline points="9 22 9 12 15 12 15 22"/></svg>',
      dashboard:   '<svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><rect x="3" y="3" width="7" height="7"/><rect x="14" y="3" width="7" height="7"/><rect x="14" y="14" width="7" height="7"/><rect x="3" y="14" width="7" height="7"/></svg>',
      exam:        '<svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M4 19.5A2.5 2.5 0 0 1 6.5 17H20"/><path d="M6.5 2H20v20H6.5A2.5 2.5 0 0 1 4 19.5v-15A2.5 2.5 0 0 1 6.5 2z"/><path d="M8 7h8"/><path d="M8 11h6"/></svg>',
      results:     '<svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><line x1="18" y1="20" x2="18" y2="10"/><line x1="12" y1="20" x2="12" y2="4"/><line x1="6" y1="20" x2="6" y2="14"/></svg>',
      practice:    '<svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M12 20h9"/><path d="M16.5 3.5a2.121 2.121 0 0 1 3 3L7 19l-4 1 1-4L16.5 3.5z"/></svg>',
      syllabus:    '<svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"/><polyline points="14 2 14 8 20 8"/><line x1="16" y1="13" x2="8" y2="13"/><line x1="16" y1="17" x2="8" y2="17"/><polyline points="10 9 9 9 8 9"/></svg>',
      review:      '<svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M1 12s4-8 11-8 11 8 11 8-4 8-11 8-11-8-11-8z"/><circle cx="12" cy="12" r="3"/></svg>',
      notify:      '<svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M18 8A6 6 0 0 0 6 8c0 7-3 9-3 9h18s-3-2-3-9"/><path d="M13.73 21a2 2 0 0 1-3.46 0"/></svg>',
      profile:     '<svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M20 21v-2a4 4 0 0 0-4-4H8a4 4 0 0 0-4 4v2"/><circle cx="12" cy="7" r="4"/></svg>',
      classes:     '<svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M17 21v-2a4 4 0 0 0-4-4H5a4 4 0 0 0-4 4v2"/><circle cx="9" cy="7" r="4"/><path d="M23 21v-2a4 4 0 0 0-3-3.87"/><path d="M16 3.13a4 4 0 0 1 0 7.75"/></svg>',
      users:       '<svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M16 21v-2a4 4 0 0 0-4-4H5a4 4 0 0 0-4 4v2"/><circle cx="8.5" cy="7" r="4"/><line x1="20" y1="8" x2="20" y2="14"/><line x1="23" y1="11" x2="17" y2="11"/></svg>',
      attendance:  '<svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><rect x="3" y="4" width="18" height="18" rx="2" ry="2"/><line x1="16" y1="2" x2="16" y2="6"/><line x1="8" y1="2" x2="8" y2="6"/><line x1="3" y1="10" x2="21" y2="10"/><polyline points="9 16 11 18 15 14"/></svg>',
      'create-exam':'<svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><circle cx="12" cy="12" r="10"/><line x1="12" y1="8" x2="12" y2="16"/><line x1="8" y1="12" x2="16" y2="12"/></svg>',
      questions:   '<svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><circle cx="12" cy="12" r="10"/><path d="M9.09 9a3 3 0 0 1 5.83 1c0 2-3 3-3 3"/><line x1="12" y1="17" x2="12.01" y2="17"/></svg>',
      reports:     '<svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"/><polyline points="14 2 14 8 20 8"/><line x1="16" y1="13" x2="8" y2="13"/><line x1="16" y1="17" x2="8" y2="17"/></svg>',
      stats:       '<svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><polyline points="23 6 13.5 15.5 8.5 10.5 1 18"/><polyline points="17 6 23 6 23 12"/></svg>',
      parents:     '<svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M20 21v-2a4 4 0 0 0-4-4H8a4 4 0 0 0-4 4v2"/><circle cx="12" cy="7" r="4"/><path d="M23 21v-2a4 4 0 0 0-3-3.87"/><path d="M16 3.13a4 4 0 0 1 0 7.75"/></svg>',
      departments: '<svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M3 9l9-7 9 7v11a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2z"/><polyline points="9 22 9 12 15 12 15 22"/></svg>',
      db:          '<svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><ellipse cx="12" cy="5" rx="9" ry="3"/><path d="M21 12c0 1.66-4 3-9 3s-9-1.34-9-3"/><path d="M3 5v14c0 1.66 4 3 9 3s9-1.34 9-3V5"/></svg>',
      login:       '<svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M15 3h4a2 2 0 0 1 2 2v14a2 2 0 0 1-2 2h-4"/><polyline points="10 17 15 12 10 7"/><line x1="15" y1="12" x2="3" y2="12"/></svg>',
      register:    '<svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M16 21v-2a4 4 0 0 0-4-4H5a4 4 0 0 0-4 4v2"/><circle cx="8.5" cy="7" r="4"/><line x1="20" y1="8" x2="20" y2="14"/><line x1="23" y1="11" x2="17" y2="11"/></svg>',
      logout:      '<svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M9 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h4"/><polyline points="16 17 21 12 16 7"/><line x1="21" y1="12" x2="9" y2="12"/></svg>',
    };
    return map[name] || '';
  }

  // ── Menu Definitions ──
  function getMenu(role) {
    if (!role) {
      var pfx = window.location.pathname.indexOf('/pages/') === 0 ? '../../' : '';
      return [
        { label: 'Home',       href: pfx + 'index.html',         icon: 'home',       group: 'explore' },
        { label: 'Browse Exams', href: pfx + 'exam-list.html',   icon: 'exam',       group: 'explore' },
        { label: 'Login',      href: pfx + 'login.html',         icon: 'login',      group: 'account' },
        { label: 'Register',   href: pfx + 'register.html',      icon: 'register',   group: 'account' },
      ];
    }

    var pfx = '/pages/' + role + '/';

    if (role === 'student') {
      return [
        { label: 'Dashboard',    href: pfx + 'dashboard.html',     icon: 'dashboard', group: 'explore' },
        { label: 'Profile & Settings', href: pfx + 'profile.html', icon: 'profile',  group: 'account' },
        { label: 'My Classes',   href: pfx + 'classes.html',       icon: 'classes',  group: 'explore' },
        { label: 'My Results',   href: pfx + 'results.html',       icon: 'results',  group: 'explore' },
        { label: 'Practice',     href: pfx + 'practice.html',       icon: 'practice', group: 'explore' },
        { label: 'Syllabus',     href: pfx + 'syllabus.html',       icon: 'syllabus', group: 'explore' },
        { label: 'Review',       href: pfx + 'review.html',         icon: 'review',   group: 'explore' },
        { label: 'Notifications',href: pfx + 'notifications.html',  icon: 'notify',   group: 'account' },
      ];
    }

    if (role === 'teacher') {
      return [
        { label: 'Dashboard',    href: pfx + 'dashboard.html',     icon: 'dashboard', group: 'explore' },
        { label: 'Profile & Settings', href: pfx + 'profile.html', icon: 'profile',  group: 'account' },
        { label: 'Classes',      href: pfx + 'classes.html',       icon: 'classes',  group: 'explore' },
        { label: 'Students',     href: pfx + 'students.html',      icon: 'users',    group: 'explore' },
        { label: 'Attendance',   href: pfx + 'attendance.html',    icon: 'attendance', group: 'explore' },
        { label: 'Syllabus',     href: pfx + 'syllabus.html',      icon: 'syllabus', group: 'explore' },
        { label: 'Questions',    href: pfx + 'questions.html',      icon: 'questions', group: 'explore' },
        { label: 'Create Exam',  href: pfx + 'create-exam.html',   icon: 'create-exam', group: 'account' },
        { label: 'Reports',      href: pfx + 'reports.html',        icon: 'reports',   group: 'account' },
        { label: 'Statistics',   href: pfx + 'statistics.html',     icon: 'stats',     group: 'account' },
        { label: 'Parent Contacts', href: pfx + 'parent-contacts.html', icon: 'parents', group: 'account' },
      ];
    }

    if (role === 'admin') {
      return [
        { label: 'Dashboard',    href: '/pages/admin/dashboard.html',  icon: 'dashboard', group: 'explore' },
        { label: 'Profile & Settings', href: '/pages/admin/profile.html', icon: 'profile', group: 'account' },
        { label: 'Users',        href: '/pages/admin/users.html',       icon: 'users',  group: 'explore' },
        { label: 'Classes',      href: '/pages/admin/classes.html',     icon: 'classes', group: 'explore' },
        { label: 'Exams',        href: '/pages/admin/exams.html',       icon: 'exam',   group: 'explore' },
        { label: 'Departments',  href: '/pages/admin/departments.html', icon: 'departments', group: 'explore' },
        { label: 'DB Monitor',   href: '/pages/admin/db-monitor.html',  icon: 'db',     group: 'account' },
      ];
    }

    return [];
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

  function createNavItem(item, currentPath) {
    var a = document.createElement('a');
    a.className = 'mobile-nav-item';
    if (item.type === 'logout') {
      a.href = '#';
      a.addEventListener('click', function (e) {
        e.preventDefault();
        closeMenu();
        if (typeof window.logout === 'function') window.logout();
        else { window.location.href = '/login.html'; }
      });
    } else {
      a.href = item.href;
      if (a.href.indexOf(currentPath) !== -1) a.classList.add('active');
      a.addEventListener('click', closeMenu);
    }

    var iconSpan = document.createElement('span');
    iconSpan.className = 'nav-icon';
    iconSpan.innerHTML = icon(item.icon);

    var labelSpan = document.createElement('span');
    labelSpan.className = 'nav-label';
    labelSpan.textContent = item.label;

    a.appendChild(iconSpan);
    a.appendChild(labelSpan);
    return a;
  }

  // ── Mega menu builders (navigation-6 style) ──
  var SPARKLE_ICON = '<svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M12 3l1.9 5.8 5.8 1.9-5.8 1.9L12 18.4l-1.9-5.8-5.8-1.9 5.8-1.9z"/><path d="M19 15l.8 2.6L22.5 18.4l-2.7.9L19 22l-.8-2.7-2.7-.9 2.7-.8z"/></svg>';
  var CHECK_ICON = '<svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M22 11.08V12a10 10 0 1 1-5.93-9.14"/><polyline points="22 4 12 14.01 9 11.01"/></svg>';
  var LAYERS_ICON = '<svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M12 2L2 7l10 5 10-5-10-5z"/><path d="M2 17l10 5 10-5"/><path d="M2 12l10 5 10-5"/></svg>';
  var BOOK_ICON = '<svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M4 19.5A2.5 2.5 0 0 1 6.5 17H20"/><path d="M6.5 2H20v20H6.5A2.5 2.5 0 0 1 4 19.5v-15A2.5 2.5 0 0 1 6.5 2z"/><path d="M8 7h8"/><path d="M8 11h6"/></svg>';
  var ARROW_UP_RIGHT = '<svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><line x1="7" y1="17" x2="17" y2="7"/><polyline points="7 7 17 7 17 17"/></svg>';

  function megaBaseHref(role) {
    return role ? '/pages/' + role + '/' : (window.location.pathname.indexOf('/pages/') === 0 ? '../../' : '');
  }

  function buildFeatureColumn(role) {
    var col = document.createElement('div');
    col.className = 'nav-mega-col';

    var iconBox = document.createElement('div');
    iconBox.className = 'nav-mega-iconbox';
    iconBox.innerHTML = SPARKLE_ICON;
    col.appendChild(iconBox);

    var h4 = document.createElement('h4');
    h4.textContent = 'AI-Powered Exams';
    col.appendChild(h4);

    var p = document.createElement('p');
    p.textContent = 'Generate questions, auto-grade answers, and get instant insights with EdTech AI.';
    col.appendChild(p);

    var base = megaBaseHref(role);
    var chips = [
      { label: 'Smart Exams',   icon: BOOK_ICON,    href: role ? base + 'dashboard.html' : base + 'exam-list.html' },
      { label: 'Auto-Grading',  icon: CHECK_ICON,   href: role ? base + (role === 'student' ? 'practice.html' : role === 'teacher' ? 'create-exam.html' : 'exams.html') : base + 'register.html' },
      { label: 'AI Question Bank', icon: LAYERS_ICON, href: role ? base + (role === 'student' ? 'syllabus.html' : role === 'teacher' ? 'questions.html' : 'departments.html') : base + 'register.html' },
    ];

    var chipsWrap = document.createElement('div');
    chips.forEach(function (chip) {
      var a = document.createElement('a');
      a.className = 'nav-mega-chip';
      a.href = chip.href;
      a.innerHTML = chip.icon + '<span>' + chip.label + '</span>';
      a.addEventListener('click', closeMenu);
      chipsWrap.appendChild(a);
    });
    col.appendChild(chipsWrap);

    return col;
  }

  function buildLinkColumn(title, menu, group) {
    var col = document.createElement('div');
    col.className = 'nav-mega-col';
    col.setAttribute('data-col', group);

    var label = document.createElement('h4');
    label.className = 'nav-mega-label';
    label.textContent = title;
    col.appendChild(label);

    var currentPath = window.location.pathname;

    menu.forEach(function (item) {
      if (item.group !== group) return;

      var a = document.createElement('a');
      a.className = 'nav-mega-link';
      a.href = item.href;
      if (a.pathname === currentPath) a.classList.add('active');
      a.innerHTML = '<span class="nav-mega-link-icon">' + icon(item.icon) + '</span><span>' + item.label + '</span>';
      a.addEventListener('click', closeMenu);
      col.appendChild(a);
    });

    return col;
  }

  function buildFeaturedColumn(role) {
    var col = document.createElement('div');
    col.className = 'nav-mega-col';

    var base = megaBaseHref(role);
    var card = document.createElement('a');
    card.className = 'nav-mega-featured';
    card.href = role ? base + 'dashboard.html' : base + 'register.html';
    card.addEventListener('click', closeMenu);

    var top = document.createElement('div');
    var badge = document.createElement('span');
    badge.className = 'nav-mega-featured-badge';
    badge.textContent = 'Upcoming Webinar';
    top.appendChild(badge);

    var h4 = document.createElement('h4');
    h4.textContent = 'Master AI exam creation';
    top.appendChild(h4);

    var p = document.createElement('p');
    p.textContent = 'Join our educators for a live teardown of the new AI proctoring & auto-grading engine.';
    top.appendChild(p);

    var cta = document.createElement('div');
    cta.className = 'nav-mega-featured-cta';
    cta.innerHTML = '<span>' + (role ? 'Open dashboard' : 'Register now') + '</span>' + ARROW_UP_RIGHT;

    card.appendChild(top);
    card.appendChild(cta);
    col.appendChild(card);

    return col;
  }

  // ── Exam-scoped links (formerly in the desktop sidebar) ──
  function buildExamTools(nav) {
    try {
      var ids = ['back-link', 'back-to-exam', 'questions-link', 'stats-link', 'attendance-link', 'reports-link'];
      var links = [];
      ids.forEach(function (id) {
        var el = document.querySelector('.sidebar #' + id);
        if (el) links.push(el);
      });
      if (!links.length) return;

      var wrap = document.createElement('div');
      wrap.className = 'nav-mega-examtools';

      var label = document.createElement('h4');
      label.className = 'nav-mega-label';
      label.textContent = 'Exam Tools';
      wrap.appendChild(label);

      links.forEach(function (a) {
        var iconSpan = a.querySelector('.nav-icon');
        var iconHtml = iconSpan ? iconSpan.innerHTML : '';
        var textSpan = a.querySelector('span:last-child');
        var text = textSpan ? textSpan.textContent.trim() : (a.textContent || '').trim();
        a.className = 'nav-mega-link';
        a.innerHTML = '<span class="nav-mega-link-icon">' + iconHtml + '</span><span>' + text + '</span>';
        a.addEventListener('click', closeMenu);
        wrap.appendChild(a);
      });

      nav.insertBefore(wrap, nav.firstChild);
    } catch (e) { /* ignore exam tools errors */ }
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

      backdrop = document.createElement('div');
      backdrop.className = 'mobile-menu-backdrop';

      // ── User header ──
      var role = null;
      var userName = null;
      try {
        role = localStorage.getItem('user_role');
        userName = localStorage.getItem('user_name');
      } catch (e) {}

      var pfx = window.location.pathname.indexOf('/pages/') === 0 ? '../../' : '';
      var menu = getMenu(role);

      var header = document.createElement('div');
      header.className = 'mobile-menu-header';

      if (role && userName) {
        var avatar = document.createElement('div');
        avatar.className = 'mobile-menu-avatar';
        avatar.textContent = userName.charAt(0).toUpperCase();

        var info = document.createElement('div');
        info.className = 'mobile-menu-user-info';

        var nameEl = document.createElement('div');
        nameEl.className = 'mobile-menu-user-name';
        nameEl.textContent = userName;

        var roleEl = document.createElement('div');
        roleEl.className = 'mobile-menu-user-role';
        roleEl.textContent = role.charAt(0).toUpperCase() + role.slice(1);

        info.appendChild(nameEl);
        info.appendChild(roleEl);
        header.appendChild(avatar);
        header.appendChild(info);

        var closeBtn = document.createElement('button');
        closeBtn.className = 'mobile-menu-close';
        closeBtn.setAttribute('aria-label', 'Close navigation menu');
        closeBtn.setAttribute('type', 'button');
        closeBtn.innerHTML = '<svg width="22" height="22" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round"><line x1="18" y1="6" x2="6" y2="18"/><line x1="6" y1="6" x2="18" y2="18"/></svg>';
        header.appendChild(closeBtn);
      } else {
        var closeBtn = document.createElement('button');
        closeBtn.className = 'mobile-menu-close';
        closeBtn.setAttribute('aria-label', 'Close navigation menu');
        closeBtn.setAttribute('type', 'button');
        closeBtn.innerHTML = '<svg width="22" height="22" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round"><line x1="18" y1="6" x2="6" y2="18"/><line x1="6" y1="6" x2="18" y2="18"/></svg>';
        header.appendChild(closeBtn);
      }

      panel.appendChild(header);

      // ── Mega menu body (navigation-6 style) ──
      var nav = document.createElement('nav');
      nav.className = 'mobile-nav-body';

      // Exam-scoped links from the (now hidden) sidebar
      buildExamTools(nav);

      var grid = document.createElement('div');
      grid.className = 'nav-mega-grid';

      // Column 1: Featured capability
      grid.appendChild(buildFeatureColumn(role));
      // Column 2: Explore links
      grid.appendChild(buildLinkColumn('Explore', menu, 'explore'));
      // Column 3: Account & resources
      grid.appendChild(buildLinkColumn('Account & Resources', menu, 'account'));
      // Column 4: Featured card
      grid.appendChild(buildFeaturedColumn(role));

      nav.appendChild(grid);

      // ── Mobile bottom CTA (navigation-6 button collection) ──
      var ctaWrap = document.createElement('div');
      ctaWrap.className = 'nav-mega-cta';

      var ctaBtn = document.createElement('a');
      ctaBtn.className = 'btn btn-primary btn-lg';
      ctaBtn.href = role ? '/pages/' + role + '/dashboard.html' : pfx + 'register.html';
      ctaBtn.textContent = role ? 'Go to Dashboard' : 'Get started';
      ctaBtn.addEventListener('click', closeMenu);
      ctaWrap.appendChild(ctaBtn);
      nav.appendChild(ctaWrap);

      panel.appendChild(nav);

      // ── Logout (appended to Account column) ──
      if (role) {
        var accountCol = panel.querySelector('[data-col="account"]');
        if (accountCol) {
          var logoutItem = document.createElement('a');
          logoutItem.className = 'nav-mega-link';
          logoutItem.href = '#';
          logoutItem.innerHTML = '<span class="nav-mega-link-icon">' + icon('logout') + '</span><span>Logout</span>';
          logoutItem.addEventListener('click', function (e) {
            e.preventDefault();
            closeMenu();
            setTimeout(function () {
              if (typeof window.logout === 'function') window.logout();
              else { window.location.href = '/login.html'; }
            }, 200);
          });
          accountCol.appendChild(logoutItem);
        }
      }

      document.body.appendChild(backdrop);
      document.body.appendChild(panel);

      // ── Wire up events ──
      btn.onclick = toggleMenu;
      backdrop.onclick = closeMenu;

      var closeBtn = header.querySelector('.mobile-menu-close');
      if (closeBtn) closeBtn.onclick = closeMenu;

      document.addEventListener('keydown', function (e) {
        if (e.key === 'Escape' && panel.classList.contains('open')) closeMenu();
      });
    } catch (e) {
      console.error('[nav] initHamburger failed:', e);
    }
  }

  // ── Desktop sidebar (same menu as hamburger) ──
  function initSidebar() {
    var sidebar = document.querySelector('.sidebar-nav');
    if (!sidebar) return;

    var role = null;
    try { role = localStorage.getItem('user_role'); } catch (e) {}

    var menu = getMenu(role);
    if (!menu.length) return;

    // Preserve exam-scoped sidebar links (page JS updates their href with the exam id)
    var preserved = [];
    ['back-link', 'back-to-exam', 'questions-link', 'stats-link', 'attendance-link', 'reports-link'].forEach(function (id) {
      var el = sidebar.querySelector('#' + id);
      if (el && el.closest('li')) preserved.push(el.closest('li'));
    });

    sidebar.innerHTML = '';

    if (preserved.length) {
      var pWrap = document.createElement('li');
      var pNav = document.createElement('nav');
      pNav.className = 'sidebar-subnav';
      preserved.forEach(function (li) { pNav.appendChild(li); });
      pWrap.appendChild(pNav);
      sidebar.appendChild(pWrap);
    }

    var currentPath = window.location.pathname;
    for (var i = 0; i < menu.length; i++) {
      var item = menu[i];
      if (item.type === 'divider') continue;
      var li = document.createElement('li');
      var a = document.createElement('a');
      a.href = item.href;
      if (a.pathname === currentPath) a.className = 'active';
      a.innerHTML = '<span class="nav-icon">' + icon(item.icon) + '</span><span>' + item.label + '</span>';
      li.appendChild(a);
      sidebar.appendChild(li);
    }

    var liLogout = document.createElement('li');
    var aLogout = document.createElement('a');
    aLogout.href = '#';
    aLogout.innerHTML = '<span class="nav-icon">🚪</span><span>Logout</span>';
    aLogout.addEventListener('click', function (e) {
      e.preventDefault();
      if (typeof window.logout === 'function') window.logout();
      else { window.location.href = '/login.html'; }
    });
    liLogout.appendChild(aLogout);
    sidebar.appendChild(liLogout);
  }

  // ── Theme toggle ──
  var SUN_ICON = '<svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><circle cx="12" cy="12" r="5"/><line x1="12" y1="1" x2="12" y2="3"/><line x1="12" y1="21" x2="12" y2="23"/><line x1="4.22" y1="4.22" x2="5.64" y2="5.64"/><line x1="18.36" y1="18.36" x2="19.78" y2="19.78"/><line x1="1" y1="12" x2="3" y2="12"/><line x1="21" y1="12" x2="23" y2="12"/><line x1="4.22" y1="19.78" x2="5.64" y2="18.36"/><line x1="18.36" y1="5.64" x2="19.78" y2="4.22"/></svg>';
  var MOON_ICON = '<svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M21 12.79A9 9 0 1 1 11.21 3 7 7 0 0 0 21 12.79z"/></svg>';

  function currentTheme() {
    try {
      var saved = localStorage.getItem('edtech-theme');
      if (saved === 'dark' || saved === 'light') return saved;
    } catch (e) {}
    return window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light';
  }

  function setTheme(t, persist) {
    var root = document.documentElement;
    if (t === 'dark') root.setAttribute('data-theme', 'dark');
    else if (t === 'light') root.setAttribute('data-theme', 'light');
    else root.removeAttribute('data-theme');

    if (persist) {
      try {
        if (t === 'dark' || t === 'light') localStorage.setItem('edtech-theme', t);
        else localStorage.removeItem('edtech-theme');
      } catch (e) {}
    }

    var meta = document.querySelector('meta[name="theme-color"]');
    if (meta) meta.content = (t === 'dark') ? '#0b0b0f' : '#be123c';

    var btn = document.querySelector('.theme-toggle');
    if (btn) {
      btn.innerHTML = (t === 'dark') ? SUN_ICON : MOON_ICON;
      btn.setAttribute('aria-label', t === 'dark' ? 'Switch to light mode' : 'Switch to dark mode');
    }
  }

  function initTheme() {
    var explicit = document.documentElement.getAttribute('data-theme');
    if (explicit === 'dark' || explicit === 'light') {
      setTheme(explicit, false);
    } else {
      setTheme(currentTheme(), false);
    }

    var navbar = document.querySelector('.navbar');
    if (!navbar) return;

    var btn = document.createElement('button');
    btn.className = 'theme-toggle';
    btn.setAttribute('type', 'button');
    btn.addEventListener('click', function () {
      setTheme(currentTheme() === 'dark' ? 'light' : 'dark', true);
    });

    navbar.appendChild(btn);
    setTheme(currentTheme(), false);

    try {
      window.matchMedia('(prefers-color-scheme: dark)').addEventListener('change', function (e) {
        if (!localStorage.getItem('edtech-theme')) setTheme(e.matches ? 'dark' : 'light', false);
      });
    } catch (err) {}
  }

  // ── Navigation-6 action buttons (search / user / CTA) ──
  var COMMAND_ICON = '<svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><circle cx="11" cy="11" r="8"/><line x1="21" y1="21" x2="16.65" y2="16.65"/></svg>';
  var USER_ICON = '<svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M20 21v-2a4 4 0 0 0-4-4H8a4 4 0 0 0-4 4v2"/><circle cx="12" cy="7" r="4"/></svg>';

  function initNavbarActions() {
    try {
      var navbar = document.querySelector('.navbar');
      if (!navbar || navbar.querySelector('.navbar-actions')) return;

      var role = null;
      try { role = localStorage.getItem('user_role'); } catch (e) {}

      var pfx = window.location.pathname.indexOf('/pages/') === 0 ? '../../' : '';
      var rolePfx = role ? '/pages/' + role + '/' : '';

      var actions = document.createElement('div');
      actions.className = 'navbar-actions';

      // Search / Browse exams
      var search = document.createElement('a');
      search.className = 'nav-action-btn';
      search.href = role ? rolePfx + 'dashboard.html' : pfx + 'exam-list.html';
      search.title = role ? 'Dashboard' : 'Browse Exams';
      search.setAttribute('aria-label', search.title);
      search.innerHTML = COMMAND_ICON;
      actions.appendChild(search);

      // User / Profile
      var user = document.createElement('a');
      user.className = 'nav-action-btn';
      user.href = role ? rolePfx + 'profile.html' : pfx + 'login.html';
      user.title = role ? 'Profile & Settings' : 'Sign In';
      user.setAttribute('aria-label', user.title);
      user.innerHTML = USER_ICON;
      actions.appendChild(user);

      // CTA
      var cta = document.createElement('a');
      cta.className = 'nav-cta-btn';
      cta.href = role ? rolePfx + 'dashboard.html' : pfx + 'register.html';
      cta.textContent = role ? 'Dashboard' : 'Get started';
      actions.appendChild(cta);

      navbar.appendChild(actions);
    } catch (e) { /* ignore navbar actions errors */ }
  }

  initPWA();
  initPageFlip();
  initLogoAnim();
  initBackButton();
  initTheme();
  initNavbarActions();
  if (!isExamPage) {
    initHamburger();
    initSidebar();
  }
})();
