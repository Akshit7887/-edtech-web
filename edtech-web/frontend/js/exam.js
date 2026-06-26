class ExamEngine {
  constructor(options = {}) {
    this.examId = options.examId || 0;
    this.sessionId = options.sessionId || 0;
    this.studentId = options.studentId || getUserId();
    this.questions = [];
    this.answers = {};
    this.currentIndex = 0;
    this.timer = null;
    this.proctor = null;
    this.mode = options.mode || 'exam';
    this.isSubmitting = false;
    this.onComplete = options.onComplete || (() => {});
  }

  async loadQuestions() {
    try {
      showLoading(true);
      const res = await api.get(`/api/questions/session/${this.studentId}/${this.examId}`);
      const data = res.data;
      this.sessionId = data.id || this.sessionId;
      this.questions = data.questions || [];
      this.questions.forEach((q, i) => {
        if (!q.id) q.id = `q_${i}`;
        if (q.answers) {
          const ans = q.answers;
          if (ans && ans.selected_answer) this.answers[q.id] = ans.selected_answer;
        }
      });
      const duration = data.time_remaining_seconds || (data.exam && data.exam.duration_minutes * 60) || 3600;
      this._setupTimer(duration);
      this._render();
      this._setupProctor();
      return data;
    } catch (e) {
      showAlert(e.message || 'Failed to load exam');
      throw e;
    } finally {
      showLoading(false);
    }
  }

  async startSession() {
    try {
      showLoading(true);
      const res = await api.post('/api/questions/create-session', {
        student_id: this.studentId,
        exam_id: this.examId,
        ip_address: '',
        user_agent: navigator.userAgent
      });
      this.sessionId = res.data.id || res.data.session_id;
      return await this.loadQuestions();
    } catch (e) {
      showAlert(e.message || 'Failed to start exam');
      throw e;
    } finally {
      showLoading(false);
    }
  }

  async submitExam() {
    if (this.isSubmitting) return;
    this.isSubmitting = true;
    if (this.proctor) this.proctor.stop();
    if (this.timer) this.timer.stop();
    try {
      const answerList = Object.entries(this.answers)
        .filter(([_, ans]) => ans)
        .map(([qId, ans]) => ({ question_id: parseInt(qId), answer: ans }));
      const res = await api.post('/api/questions/submit', {
        session_id: this.sessionId,
        answers: answerList
      });
      this.onComplete(res.data);
      return res.data;
    } catch (e) {
      this.isSubmitting = false;
      showAlert(e.message || 'Failed to submit exam');
      throw e;
    }
  }

  selectAnswer(questionId, answer) {
    this.answers[questionId] = answer;
    this._updateNavButtons();
    this._updateProgress();
  }

  goToQuestion(index) {
    if (index >= 0 && index < this.questions.length) {
      this.currentIndex = index;
      this._renderQuestion();
      this._updateNavButtons();
    }
  }

  nextQuestion() { this.goToQuestion(this.currentIndex + 1); }
  prevQuestion() { this.goToQuestion(this.currentIndex - 1); }

  getAnsweredCount() { return Object.values(this.answers).filter(a => a).length; }

  _setupTimer(durationSeconds) {
    const timerEl = document.getElementById('exam-timer');
    if (!timerEl) return;
    this.timer = ExamTimer.fromMinutes(Math.ceil(durationSeconds / 60), (remaining, state) => {
      timerEl.textContent = `Time Remaining: ${this.timer.formatTime(remaining)}`;
      timerEl.className = 'exam-timer';
      if (state === 'warning') timerEl.classList.add('warning');
      if (state === 'danger') timerEl.classList.add('danger');
    }, () => {
      showAlert('Time is up! Submitting automatically...', 'warning');
      this.submitExam();
    });
    this.timer.start();
  }

  _setupProctor() {
    if (this.mode !== 'exam') return;
    this.proctor = new Proctor({
      onViolation: (v) => {
        console.warn('Proctor violation', v);
        const el = document.getElementById('proctor-warnings');
        if (el) el.textContent = `Warning ${v.count}/${v.maxWarnings}: ${v.reason}`;
      },
      onDisqualify: async (d) => {
        showAlert(`Disqualified: ${d.reason}`, 'error');
        try {
          await api.post(`/api/questions/disqualify/${this.sessionId}`, { reason: d.reason });
        } catch (e) {}
        setTimeout(() => goTo('/pages/student/dashboard.html'), 2000);
      }
    });
    this.proctor.start();
  }

  _render() {
    this._buildNavigator();
    this._renderQuestion();
    this._updateNavButtons();
    this._updateProgress();
  }

  _renderQuestion() {
    const container = document.getElementById('question-container');
    if (!container || !this.questions.length) return;
    const q = this.questions[this.currentIndex];
    const idx = this.currentIndex;
    const letters = ['A', 'B', 'C', 'D'];
    const opts = [q.option_a, q.option_b, q.option_c, q.option_d].filter(Boolean);

    container.innerHTML = `
      <div class="question-card">
        <div class="question-number">${idx + 1}</div>
        <div class="question-text">${q.question_text}</div>
        <div class="options">
          ${opts.map((opt, oi) => {
            const val = letters[oi];
            const selected = this.answers[q.id] === val;
            return `
              <label class="option-label ${selected ? 'selected' : ''}">
                <input type="radio" name="q_${q.id}" value="${val}" ${selected ? 'checked' : ''} onchange="examEngine.selectAnswer(${q.id}, '${val}')">
                <span class="option-prefix">${val}.</span>
                <span>${opt}</span>
              </label>
            `;
          }).join('')}
        </div>
      </div>
      <div class="exam-nav" style="display:flex;justify-content:space-between;margin-top:16px;">
        <button class="btn btn-secondary" onclick="examEngine.prevQuestion()" ${idx === 0 ? 'disabled' : ''}>← Previous</button>
        <button class="btn btn-secondary" onclick="examEngine.nextQuestion()" ${idx === this.questions.length - 1 ? 'disabled' : ''}>Next →</button>
      </div>
    `;
  }

  _buildNavigator() {
    const nav = document.getElementById('question-navigator');
    if (!nav) return;
    nav.innerHTML = '<div style="font-size:0.75rem;color:var(--text-dim);text-align:center;margin-bottom:6px;">Questions</div>' +
      this.questions.map((q, i) =>
        `<button class="q-nav-btn ${this.answers[q.id] ? 'answered' : ''} ${i === this.currentIndex ? 'current' : ''}" onclick="examEngine.goToQuestion(${i})">${i + 1}</button>`
      ).join('');
  }

  _updateNavButtons() {
    const nav = document.getElementById('question-navigator');
    if (!nav) return;
    nav.querySelectorAll('.q-nav-btn').forEach((btn, i) => {
      const q = this.questions[i];
      btn.className = 'q-nav-btn';
      if (this.answers[q && q.id]) btn.classList.add('answered');
      if (i === this.currentIndex) btn.classList.add('current');
    });
  }

  _updateProgress() {
    const bar = document.getElementById('progress-fill');
    const text = document.getElementById('progress-text');
    if (!bar || !text) return;
    const total = this.questions.length;
    const answered = this.getAnsweredCount();
    const pct = total > 0 ? (answered / total) * 100 : 0;
    bar.style.width = `${pct}%`;
    text.textContent = `${answered}/${total} answered`;
  }
}

async function loadMyResults() {
  const sid = getUserId();
  try {
    const res = await api.get(`/api/questions/my-results/${sid}`);
    return res.data;
  } catch (e) {
    showAlert(e.message || 'Failed to load results');
    return [];
  }
}

async function loadExamReview(sessionId) {
  try {
    const res = await api.get(`/api/students/review/${sessionId}`);
    return res.data;
  } catch (e) {
    showAlert(e.message || 'Failed to load review');
    return null;
  }
}
