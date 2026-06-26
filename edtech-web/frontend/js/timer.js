class ExamTimer {
  constructor(durationSeconds, onTick, onExpire) {
    this.totalSeconds = durationSeconds;
    this.remaining = durationSeconds;
    this.onTick = onTick || (() => {});
    this.onExpire = onExpire || (() => {});
    this.interval = null;
    this.running = false;
    this.warningThreshold = 300;
    this.dangerThreshold = 60;
  }

  start() {
    if (this.running) return;
    this.running = true;
    this.interval = setInterval(() => {
      this.remaining--;
      if (this.remaining <= 0) {
        this.remaining = 0;
        this.stop();
        this.onExpire();
      }
      this.onTick(this.remaining, this.getState());
    }, 1000);
  }

  stop() {
    this.running = false;
    if (this.interval) { clearInterval(this.interval); this.interval = null; }
  }

  getRemaining() { return this.remaining; }
  getState() {
    if (this.remaining <= this.dangerThreshold) return 'danger';
    if (this.remaining <= this.warningThreshold) return 'warning';
    return 'safe';
  }

  formatTime(seconds) {
    const s = seconds !== undefined ? seconds : this.remaining;
    const h = Math.floor(s / 3600);
    const m = Math.floor((s % 3600) / 60);
    const sec = s % 60;
    if (h > 0) return `${h}h ${m}m ${sec}s`;
    if (m > 0) return `${m}m ${sec}s`;
    return `${sec}s`;
  }

  static fromMinutes(minutes, onTick, onExpire) {
    return new ExamTimer(minutes * 60, onTick, onExpire);
  }
}
