const API_BASE = (() => {
  const stored = localStorage.getItem('api_base');
  if (stored) return stored.replace(/\/+$/, '');
  const host = window.location.hostname;
  if (host !== 'localhost' && host !== '127.0.0.1') {
    return 'https://edtech-web-rptw.onrender.com';
  }
  return 'http://localhost:5000';
})();

class ApiClient {
  constructor() {
    this.token = localStorage.getItem('token');
  }

  setToken(token) {
    this.token = token;
    if (token) localStorage.setItem('token', token);
    else localStorage.removeItem('token');
  }

  getToken() { return this.token; }

  getHeaders(isForm = false) {
    const h = {};
    if (!isForm) h['Content-Type'] = 'application/json';
    if (this.token) h['Authorization'] = `Bearer ${this.token}`;
    if (API_BASE.includes('ngrok')) h['ngrok-skip-browser-warning'] = 'true';
    return h;
  }

  async request(method, path, body = null) {
    const opts = { method, headers: this.getHeaders() };
    if (body !== null) opts.body = JSON.stringify(body);
    const url = `${API_BASE}${path}`;
    try {
      const res = await fetch(url, opts);
      const contentType = res.headers.get('content-type') || '';
      let data = null;
      if (contentType.includes('application/json')) {
        data = await res.json();
      } else if (contentType.includes('application/pdf')) {
        const blob = await res.blob();
        return { ok: res.ok, blob, headers: res.headers };
      } else {
        const text = await res.text();
        return { ok: res.ok, text, status: res.status };
      }
      if (!res.ok) {
        const err = (data && data.error) || `Request failed (${res.status})`;
        throw new ApiError(err, res.status, data);
      }
      return data;
    } catch (e) {
      if (e instanceof ApiError) throw e;
      throw new ApiError(e.message || 'Network error', 0);
    }
  }

  get(path) { return this.request('GET', path); }
  post(path, body = {}) { return this.request('POST', path, body); }
  put(path, body = {}) { return this.request('PUT', path, body); }
  del(path) { return this.request('DELETE', path); }

  async uploadFile(path, formData) {
    const opts = { method: 'POST', headers: this.getHeaders(true), body: formData };
    const r = await fetch(`${API_BASE}${path}`, opts);
    const data = await r.json();
    if (!r.ok) {
      const err = (data && data.message) || (data && data.error) || `Upload failed (${r.status})`;
      throw new ApiError(err, r.status, data);
    }
    return data;
  }

  downloadPdf(path) {
    const opts = { method: 'GET', headers: this.getHeaders() };
    return fetch(`${API_BASE}${path}`, opts).then(async r => {
      if (!r.ok) {
        const err = await r.json().catch(() => ({ error: 'Download failed' }));
        throw new ApiError(err.error || 'Download failed', r.status);
      }
      const blob = await r.blob();
      const url = URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url; a.download = path.split('/').pop() + '.pdf';
      document.body.appendChild(a); a.click();
      a.remove(); URL.revokeObjectURL(url);
    });
  }
}

class ApiError extends Error {
  constructor(message, status, data = null) {
    super(message);
    this.status = status;
    this.data = data;
  }
}

const api = new ApiClient();

function showAlert(msg, type = 'error', container) {
  const el = document.createElement('div');
  el.className = `alert alert-${type}`;
  el.textContent = msg;
  const parent = container || document.querySelector('.container') || document.body;
  parent.prepend(el);
  if (type !== 'error') {
    setTimeout(() => { if (el.parentNode) el.remove(); }, 4000);
  }
}

function clearAlerts() {
  document.querySelectorAll('.alert').forEach(el => el.remove());
}

function showLoading(show = true) {
  let overlay = document.getElementById('loading-overlay');
  if (!show) { if (overlay) overlay.remove(); return; }
  if (overlay) return;
  overlay = document.createElement('div');
  overlay.id = 'loading-overlay';
  overlay.className = 'loading-overlay';
  overlay.innerHTML = '<div class="spinner"></div><span>Loading...</span>';
  const main = document.querySelector('.main-content') || document.querySelector('.container') || document.body;
  main.prepend(overlay);
}

function formatDate(d) {
  if (!d) return '-';
  return new Date(d).toLocaleDateString('en-IN', { day: 'numeric', month: 'short', year: 'numeric', hour: '2-digit', minute: '2-digit' });
}

function statusBadge(status) {
  const map = {
    active: 'badge-green',
    draft: 'badge-yellow',
    closed: 'badge-gray',
    completed: 'badge-green',
    in_progress: 'badge-yellow',
    not_started: 'badge-gray',
    disqualified: 'badge-red',
    present: 'badge-green',
    absent: 'badge-red'
  };
  const cls = map[status] || 'badge-gray';
  return `<span class="badge ${cls}">${status.replace(/_/g, ' ')}</span>`;
}

function capitalize(s) { return s ? s.charAt(0).toUpperCase() + s.slice(1) : ''; }

function getQueryParam(name) {
  const p = new URLSearchParams(window.location.search);
  return p.get(name) || '';
}

function goTo(path) { window.location.href = path; }

function confirmAction(msg) { return new Promise(resolve => { resolve(confirm(msg)); }); }
