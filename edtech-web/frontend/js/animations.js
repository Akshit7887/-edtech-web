/* ============================================================
   EdTech Web - Animation & Interaction Engine
   Minimal scroll-triggered reveals, micro-interactions
   ============================================================ */

(function () {
  'use strict';

  // ── Intersection Observer for scroll reveals ──
  function initScrollReveal() {
    const targets = document.querySelectorAll('.reveal, .reveal-left, .reveal-right, .reveal-scale');

    if (targets.length === 0) return;

    const observer = new IntersectionObserver(
      (entries) => {
        entries.forEach((entry) => {
          if (entry.isIntersecting) {
            entry.target.classList.add('revealed');
            observer.unobserve(entry.target);
          }
        });
      },
      {
        threshold: 0.1,
        rootMargin: '0px 0px -40px 0px',
      }
    );

    targets.forEach((el) => observer.observe(el));
  }

  // ── Stagger children reveal ──
  function initStaggerReveal() {
    document.querySelectorAll('.stagger-group').forEach((group) => {
      const children = group.children;
      Array.from(children).forEach((child, i) => {
        child.style.setProperty('--stagger-delay', `${i * 0.08}s`);
        child.classList.add('reveal');
      });
    });
  }

  // ── Navbar shrink on scroll ──
  function initNavbarScroll() {
    const nav = document.querySelector('.navbar');
    if (!nav) return;

    const observer = new IntersectionObserver(
      ([e]) => {
        nav.classList.toggle('scrolled', !e.isIntersecting);
      },
      { threshold: 0, rootMargin: '-72px 0px 0px 0px' }
    );

    const sentinel = document.createElement('div');
    sentinel.style.position = 'absolute';
    sentinel.style.top = '0';
    sentinel.style.left = '0';
    sentinel.style.width = '1px';
    sentinel.style.height = '1px';
    sentinel.style.pointerEvents = 'none';
    document.body.prepend(sentinel);
    observer.observe(sentinel);
  }

  // ── Smooth anchor links ──
  function initSmoothScroll() {
    document.querySelectorAll('a[href^="#"]').forEach((anchor) => {
      anchor.addEventListener('click', (e) => {
        const href = anchor.getAttribute('href');
        if (!href || href === '#') return;
        const target = document.querySelector(href);
        if (target) {
          e.preventDefault();
          target.scrollIntoView({ behavior: 'smooth', block: 'start' });
        }
      });
    });
  }

  // ── Counter animation ──
  function initCounters() {
    document.querySelectorAll('.counter').forEach((el) => {
      const target = parseInt(el.dataset.target, 10);
      if (isNaN(target)) return;
      const suffix = el.dataset.suffix || '';
      const duration = parseInt(el.dataset.duration, 10) || 1500;
      const startTime = performance.now();

      function update(currentTime) {
        const elapsed = currentTime - startTime;
        const progress = Math.min(elapsed / duration, 1);
        const eased = 1 - Math.pow(1 - progress, 3);
        const current = Math.round(eased * target);
        const formatted = el.dataset.format === 'locale' ? current.toLocaleString('en-US') : current;
        el.textContent = formatted + suffix;
        if (progress < 1) requestAnimationFrame(update);
      }

      const observer = new IntersectionObserver(
        ([entry]) => {
          if (entry.isIntersecting) {
            requestAnimationFrame(update);
            observer.unobserve(el);
          }
        },
        { threshold: 0.5 }
      );
      observer.observe(el);
    });
  }

  // ── Active nav link on scroll ──
  function initActiveNav() {
    const sections = document.querySelectorAll('section[id]');
    const navLinks = document.querySelectorAll('.navbar-nav a[href^="#"]');
    if (sections.length === 0 || navLinks.length === 0) return;

    const observer = new IntersectionObserver(
      (entries) => {
        entries.forEach((entry) => {
          if (entry.isIntersecting) {
            const id = entry.target.getAttribute('id');
            navLinks.forEach((link) => {
              link.classList.toggle('active', link.getAttribute('href') === `#${id}`);
            });
          }
        });
      },
      { threshold: 0.3 }
    );

    sections.forEach((s) => observer.observe(s));
  }

  // ── Toast notification system ──
  window.showToast = function (message, type) {
    const existing = document.querySelector('.toast');
    if (existing) existing.remove();

    const toast = document.createElement('div');
    toast.className = `toast toast-${type || 'success'}`;
    toast.innerHTML = `<span>${message}</span>`;
    document.body.appendChild(toast);

    setTimeout(() => {
      toast.style.opacity = '0';
      toast.style.transform = 'translateY(-16px) scale(0.95)';
      toast.style.transition = 'opacity 0.3s ease, transform 0.3s ease';
      setTimeout(() => toast.remove(), 300);
    }, 3000);
  };

  // ── Copy to clipboard ──
  window.copyToClipboard = async function (text) {
    try {
      await navigator.clipboard.writeText(text);
      showToast('Copied to clipboard', 'success');
    } catch {
      const ta = document.createElement('textarea');
      ta.value = text;
      ta.style.position = 'fixed';
      ta.style.opacity = '0';
      document.body.appendChild(ta);
      ta.select();
      document.execCommand('copy');
      ta.remove();
      showToast('Copied to clipboard', 'success');
    }
  };

  // ── Typewriter effect ──
  function initTypewriters() {
    document.querySelectorAll('[data-typer]').forEach(function (el) {
      var words = (el.getAttribute('data-words') || '').split(',').map(function (w) { return w.trim(); }).filter(Boolean);
      if (words.length === 0) {
        var initial = el.textContent.trim();
        if (!initial) return;
        words = [initial];
      }
      var typeSpeed = parseInt(el.getAttribute('data-type-speed'), 10) || 65;
      var pause = parseInt(el.getAttribute('data-pause'), 10) || 1700;
      var caret = document.createElement('span');
      caret.className = 'typer-caret';
      caret.setAttribute('aria-hidden', 'true');

      var reduced = window.matchMedia && window.matchMedia('(prefers-reduced-motion: reduce)').matches;
      if (reduced) { el.textContent = words[0]; return; }

      el.textContent = '';
      var textNode = document.createTextNode('');
      el.appendChild(textNode);
      el.appendChild(caret);

      var wordIndex = 0;
      var charIndex = 0;
      var deleting = false;

      function tick() {
        var word = words[wordIndex];
        if (deleting) {
          charIndex--;
          textNode.nodeValue = word.slice(0, charIndex);
          if (charIndex <= 0) { deleting = false; wordIndex = (wordIndex + 1) % words.length; }
        } else {
          charIndex++;
          textNode.nodeValue = word.slice(0, charIndex);
          if (charIndex >= word.length) deleting = true;
        }
        var delay = deleting ? typeSpeed / 2 : (charIndex >= word.length ? pause : typeSpeed);
        setTimeout(tick, delay);
      }
      tick();
    });
  }

  // ── Live status pills ──
  function initLivePills() {
    document.querySelectorAll('[data-live]').forEach(function (el) {
      if (el.querySelector('.live-dot')) return;
      var label = el.getAttribute('data-live') || el.textContent.trim() || 'LIVE';
      el.setAttribute('role', 'status');
      el.textContent = '';
      var dot = document.createElement('span');
      dot.className = 'live-dot';
      dot.setAttribute('aria-hidden', 'true');
      var lbl = document.createElement('span');
      lbl.className = 'live-label';
      lbl.textContent = label;
      el.appendChild(dot);
      el.appendChild(lbl);
    });
  }

  // ── 3D tilt on hover ──
  function initTilts() {
    if (window.matchMedia && window.matchMedia('(prefers-reduced-motion: reduce)').matches) return;
    document.querySelectorAll('[data-tilt]').forEach(function (el) {
      var max = parseFloat(el.getAttribute('data-tilt-max')) || 6;
      var rect = null;
      el.addEventListener('mouseenter', function () { rect = el.getBoundingClientRect(); });
      el.addEventListener('mousemove', function (e) {
        if (!rect) return;
        var px = (e.clientX - rect.left) / rect.width - 0.5;
        var py = (e.clientY - rect.top) / rect.height - 0.5;
        el.style.transform = 'perspective(900px) rotateX(' + (-py * max).toFixed(2) + 'deg) rotateY(' + (px * max).toFixed(2) + 'deg)';
      });
      el.addEventListener('mouseleave', function () { el.style.transform = ''; rect = null; });
    });
  }

  // ── Button ripple ──
  function initRipples() {
    document.querySelectorAll('.btn-ripple').forEach(function (btn) {
      btn.addEventListener('click', function (e) {
        var rect = btn.getBoundingClientRect();
        var size = Math.max(rect.width, rect.height) * 2;
        var ripple = document.createElement('span');
        ripple.className = 'ripple';
        ripple.style.width = ripple.style.height = size + 'px';
        ripple.style.left = (e.clientX - rect.left - size / 2) + 'px';
        ripple.style.top = (e.clientY - rect.top - size / 2) + 'px';
        btn.appendChild(ripple);
        ripple.addEventListener('animationend', function () { ripple.remove(); });
      });
    });
  }

  // ── Animated counter (for dynamically-set stats) ──
  window.animateCount = function (el, target, opts) {
    if (!el) return;
    opts = opts || {};
    var suffix = opts.suffix || '';
    var prefix = opts.prefix || '';
    var duration = opts.duration || 1200;
    var startTime = performance.now();
    var from = 0;
    var reduced = window.matchMedia && window.matchMedia('(prefers-reduced-motion: reduce)').matches;

    function fmt(n) { return Math.round(n).toLocaleString('en-US'); }
    if (reduced) { el.textContent = prefix + fmt(target) + suffix; return; }

    function frame(now) {
      var p = Math.min((now - startTime) / duration, 1);
      var eased = 1 - Math.pow(1 - p, 3);
      el.textContent = prefix + fmt(from + (target - from) * eased) + suffix;
      if (p < 1) requestAnimationFrame(frame);
    }
    requestAnimationFrame(frame);
  };

  // ── Knowledge Carousel ──
  var carouselState = {};

  function initCarousel() {
    var tracks = document.querySelectorAll('.carousel-track[data-carousel]');
    if (tracks.length === 0) { var t = document.querySelector('.carousel-track'); if (t) t.setAttribute('data-carousel', 'default'); tracks = document.querySelectorAll('.carousel-track[data-carousel]'); }

    tracks.forEach(function (track) {
      var name = track.getAttribute('data-carousel') || 'default';
      var dotsContainer = document.querySelector('.carousel-dots[data-dots="' + name + '"]');
      if (!dotsContainer) return;

      var cards = track.querySelectorAll('.carousel-card');
      if (cards.length === 0) return;

      carouselState[name] = { index: 0, interval: null };

      dotsContainer.innerHTML = '';
      cards.forEach(function (_, i) {
        var dot = document.createElement('button');
        dot.className = 'carousel-dot' + (i === 0 ? ' active' : '');
        dot.setAttribute('aria-label', 'Slide ' + (i + 1));
        dot.onclick = function () { goToSlide(i, name); };
        dotsContainer.appendChild(dot);
      });

      cards[0].classList.add('active');
      startAutoSlide(name);

      track.addEventListener('mouseenter', function () { stopAutoSlide(name); });
      track.addEventListener('mouseleave', function () { startAutoSlide(name); });
    });
  }

  window.slideCarousel = function (direction, name) {
    name = name || 'default';
    var track = document.querySelector('.carousel-track[data-carousel="' + name + '"]');
    if (!track) return;
    var cards = track.querySelectorAll('.carousel-card');
    var total = cards.length;
    var state = carouselState[name];
    if (!state) return;
    state.index = (state.index + direction + total) % total;
    updateCarousel(track, cards, name);
  };

  function goToSlide(index, name) {
    name = name || 'default';
    var track = document.querySelector('.carousel-track[data-carousel="' + name + '"]');
    if (!track) return;
    var cards = track.querySelectorAll('.carousel-card');
    var state = carouselState[name];
    if (!state) return;
    state.index = index;
    updateCarousel(track, cards, name);
  }

  function updateCarousel(track, cards, name) {
    var state = carouselState[name];
    if (!state) return;
    track.style.transform = 'translateX(-' + (state.index * 100) + '%)';
    cards.forEach(function (c, i) { c.classList.toggle('active', i === state.index); });
    var dots = document.querySelectorAll('.carousel-dots[data-dots="' + name + '"] .carousel-dot');
    dots.forEach(function (d, i) { d.classList.toggle('active', i === state.index); });
    resetAutoSlide(name);
  }

  function startAutoSlide(name) {
    stopAutoSlide(name);
    var state = carouselState[name];
    if (!state) return;
    state.interval = setInterval(function () {
      var track = document.querySelector('.carousel-track[data-carousel="' + name + '"]');
      if (!track) return;
      var cards = track.querySelectorAll('.carousel-card');
      var total = cards.length;
      state.index = (state.index + 1) % total;
      updateCarousel(track, cards, name);
    }, 4000);
  }

  function stopAutoSlide(name) {
    var state = carouselState[name];
    if (state && state.interval) { clearInterval(state.interval); state.interval = null; }
  }

  function resetAutoSlide(name) { startAutoSlide(name); }

  // ── Init on DOM ready ──
  function init() {
    initNavbarScroll();
    initStaggerReveal();
    initScrollReveal();
    initSmoothScroll();
    initCounters();
    initActiveNav();
    initCarousel();
    initTypewriters();
    initLivePills();
    initTilts();
    initRipples();
  }

  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', init);
  } else {
    init();
  }
})();
