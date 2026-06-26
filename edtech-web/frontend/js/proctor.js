class Proctor {
  constructor(options = {}) {
    this.warningCount = 0;
    this.maxWarnings = options.maxWarnings || 3;
    this.onViolation = options.onViolation || (() => {});
    this.onDisqualify = options.onDisqualify || (() => {});
    this.enabled = options.enabled !== false;
    this.fullscreenRequired = options.fullscreenRequired !== false;
    this.monitorFocus = options.monitorFocus !== false;
    this.monitorVisibility = options.monitorVisibility !== false;
    this.monitorRightClick = options.monitorRightClick !== false;
    this.monitorKeyCombos = options.monitorKeyCombos !== false;
    this.warnings = [];
    this.isDisqualified = false;
    this._handlers = {};
  }

  start() {
    if (!this.enabled) return;
    if (this.fullscreenRequired) this._requestFullscreen();
    if (this.monitorFocus) this._setupFocusMonitor();
    if (this.monitorVisibility) this._setupVisibilityMonitor();
    if (this.monitorRightClick) this._setupRightClickBlock();
    if (this.monitorKeyCombos) this._setupKeyComboBlock();
  }

  stop() {
    Object.entries(this._handlers).forEach(([ev, fn]) => {
      document.removeEventListener(ev, fn);
    });
    this._handlers = {};
    if (document.fullscreenElement) document.exitFullscreen().catch(() => {});
  }

  _requestFullscreen() {
    const el = document.documentElement;
    if (el.requestFullscreen && !document.fullscreenElement) {
      el.requestFullscreen().catch(() => {});
    }
  }

  _setupFocusMonitor() {
    const handler = () => {
      if (this.isDisqualified) return;
      if (!document.hasFocus()) {
        this._addWarning('Browser focus lost');
      }
    };
    window.addEventListener('blur', handler);
    this._handlers['blur'] = handler;
  }

  _setupVisibilityMonitor() {
    const handler = () => {
      if (this.isDisqualified) return;
      if (document.hidden) {
        this._addWarning('Tab switched');
      }
    };
    document.addEventListener('visibilitychange', handler);
    this._handlers['visibilitychange'] = handler;
  }

  _setupRightClickBlock() {
    const handler = (e) => {
      e.preventDefault();
      this._addWarning('Right-click attempted');
      return false;
    };
    document.addEventListener('contextmenu', handler);
    this._handlers['contextmenu'] = handler;
  }

  _setupKeyComboBlock() {
    const handler = (e) => {
      if (this.isDisqualified) return;
      if ((e.ctrlKey || e.metaKey) && (e.key === 'c' || e.key === 'v' || e.key === 'x' || e.key === 'a' || e.key === 'p' || e.key === 'u')) {
        e.preventDefault();
        this._addWarning(`Key combo (${e.key}) blocked`);
      }
      if ((e.ctrlKey || e.metaKey) && e.shiftKey && e.key === 'i') {
        e.preventDefault();
        this._addWarning('Dev tools attempt blocked');
      }
      if (e.key === 'F12') {
        e.preventDefault();
        this._addWarning('F12 blocked');
      }
    };
    document.addEventListener('keydown', handler);
    this._handlers['keydown'] = handler;
  }

  _addWarning(reason) {
    this.warningCount++;
    this.warnings.push({ reason, time: new Date().toISOString() });
    this.onViolation({ count: this.warningCount, reason, maxWarnings: this.maxWarnings });
    if (this.warningCount >= this.maxWarnings) {
      this.disqualify(reason);
    }
  }

  disqualify(reason) {
    this.isDisqualified = true;
    this.stop();
    this.onDisqualify({ reason, warnings: this.warnings });
  }

  getWarnings() { return this.warnings; }
  getWarningCount() { return this.warningCount; }
}
