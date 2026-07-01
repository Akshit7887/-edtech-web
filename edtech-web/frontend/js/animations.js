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
        el.textContent = current + suffix;
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

  // ── Init on DOM ready ──
  function init() {
    initNavbarScroll();
    initStaggerReveal();
    initScrollReveal();
    initSmoothScroll();
    initCounters();
    initActiveNav();
  }

  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', init);
  } else {
    init();
  }
})();
