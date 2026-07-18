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

  // ── Page Flip Transitions ──
  function initPageFlip() {
    var isInternal = document.referrer && document.referrer.indexOf(window.location.host) !== -1;
    if (isInternal) {
      document.body.classList.add('page-flip-in');
      setTimeout(function () {
        document.body.classList.remove('page-flip-in');
      }, 500);
    }

    if (typeof window.goTo === 'function') {
      var orig = window.goTo;
      window.goTo = function (path) {
        document.body.classList.remove('page-flip-in');
        document.body.classList.add('page-flip-out');
        setTimeout(function () { orig(path); }, 300);
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
        { label: 'Home',       href: pfx + 'index.html',         icon: 'home' },
        { label: 'Browse Exams', href: pfx + 'exam-list.html',   icon: 'exam' },
        { type: 'divider' },
        { label: 'Login',      href: pfx + 'login.html',         icon: 'login' },
        { label: 'Register',   href: pfx + 'register.html',      icon: 'register' },
      ];
    }

    var pfx = '/pages/' + role + '/';

    if (role === 'student') {
      return [
        { label: 'Dashboard',    href: pfx + 'dashboard.html',     icon: 'dashboard' },
        { type: 'divider' },
        { label: 'My Results',   href: pfx + 'results.html',       icon: 'results' },
        { label: 'Practice',     href: pfx + 'practice.html',       icon: 'practice' },
        { label: 'Syllabus',     href: pfx + 'syllabus.html',       icon: 'syllabus' },
        { label: 'Review',       href: pfx + 'review.html',         icon: 'review' },
        { type: 'divider' },
        { label: 'Notifications',href: pfx + 'notifications.html',  icon: 'notify' },
        { label: 'Profile',      href: pfx + 'profile.html',        icon: 'profile' },
      ];
    }

    if (role === 'teacher') {
      return [
        { label: 'Dashboard',    href: pfx + 'dashboard.html',     icon: 'dashboard' },
        { type: 'divider' },
        { label: 'Classes',      href: pfx + 'classes.html',       icon: 'classes' },
        { label: 'Students',     href: pfx + 'students.html',      icon: 'users' },
        { label: 'Attendance',   href: pfx + 'attendance.html',    icon: 'attendance' },
        { label: 'Syllabus',     href: pfx + 'syllabus.html',      icon: 'syllabus' },
        { type: 'divider' },
        { label: 'Create Exam',  href: pfx + 'create-exam.html',   icon: 'create-exam' },
        { label: 'Questions',    href: pfx + 'questions.html',      icon: 'questions' },
        { label: 'Reports',      href: pfx + 'reports.html',        icon: 'reports' },
        { label: 'Statistics',   href: pfx + 'statistics.html',     icon: 'stats' },
        { type: 'divider' },
        { label: 'Parent Contacts', href: pfx + 'parent-contacts.html', icon: 'parents' },
      ];
    }

    if (role === 'admin') {
      return [
        { label: 'Dashboard',    href: '/pages/admin/dashboard.html',  icon: 'dashboard' },
        { type: 'divider' },
        { label: 'Users',        href: '/pages/admin/users.html',       icon: 'users' },
        { label: 'Exams',        href: '/pages/admin/exams.html',       icon: 'exam' },
        { label: 'Departments',  href: '/pages/admin/departments.html', icon: 'departments' },
        { label: 'DB Monitor',   href: '/pages/admin/db-monitor.html',  icon: 'db' },
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

      // ── Nav items ──
      var nav = document.createElement('nav');
      nav.className = 'mobile-nav-body';

      var menu = getMenu(role);
      var currentPath = window.location.pathname;

      for (var i = 0; i < menu.length; i++) {
        var item = menu[i];
        if (item.type === 'divider') {
          var div = document.createElement('div');
          div.className = 'mobile-nav-divider';
          nav.appendChild(div);
        } else {
          nav.appendChild(createNavItem(item, currentPath));
        }
      }

      // ── Logout button (if logged in) ──
      if (role) {
        var div = document.createElement('div');
        div.className = 'mobile-nav-divider';
        nav.appendChild(div);

        var logoutItem = document.createElement('a');
        logoutItem.className = 'mobile-nav-item mobile-nav-logout';
        logoutItem.href = '#';
        logoutItem.innerHTML = '<span class="nav-icon">' + icon('logout') + '</span><span class="nav-label">Logout</span>';
        logoutItem.addEventListener('click', function (e) {
          e.preventDefault();
          closeMenu();
          setTimeout(function () {
            if (typeof window.logout === 'function') window.logout();
            else { window.location.href = '/login.html'; }
          }, 200);
        });
        nav.appendChild(logoutItem);
      }

      panel.appendChild(nav);

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

  initPWA();
  initPageFlip();
  initLogoAnim();
  initBackButton();
  if (!isExamPage) {
    initHamburger();
  }
})();
