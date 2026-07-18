async function loginRequest(identifier, password, role) {
  const res = await api.post('/api/auth/generate-otp', { identifier, password, role });
  return res.data;
}

async function verifyOtpRequest(identifier, otpCode, role) {
  const res = await api.post('/api/auth/verify-otp', { identifier, otp_code: otpCode, role });
  return res.data;
}

async function sendRegisterOtp(name, identifier, password, role) {
  const res = await api.post('/api/auth/send-register-otp', { name, identifier, password, role });
  return res;
}

async function verifyRegisterOtp(identifier, otpCode) {
  const res = await api.post('/api/auth/verify-register-otp', { identifier, otp_code: otpCode });
  return res.data;
}

async function forgotPassword(identifier) {
  const res = await api.post('/api/auth/forgot-password', { identifier });
  return res;
}

async function resetPassword(identifier, otpCode, newPassword) {
  const res = await api.post('/api/auth/reset-password', { identifier, otp_code: otpCode, new_password: newPassword });
  return res;
}

async function updateProfile(data) {
  const res = await api.put('/api/auth/profile', data);
  return res.data;
}

async function changePassword(currentPassword, newPassword) {
  const res = await api.post('/api/auth/change-password', { current_password: currentPassword, new_password: newPassword });
  return res.data;
}

async function deleteProfile() {
  const res = await api.del('/api/auth/profile');
  return res;
}

function saveSession(token, user) {
  api.setToken(token);
  localStorage.setItem('user', JSON.stringify(user));
  localStorage.setItem('user_role', user.role);
  localStorage.setItem('user_id', user.id);
  localStorage.setItem('user_name', user.name);
  if (user.student_id) localStorage.setItem('user_student_id', user.student_id);
}

function clearSession() {
  api.setToken(null);
  localStorage.removeItem('user');
  localStorage.removeItem('user_role');
  localStorage.removeItem('user_id');
  localStorage.removeItem('user_name');
  localStorage.removeItem('user_student_id');
}

function getUser() {
  const u = localStorage.getItem('user');
  return u ? JSON.parse(u) : null;
}

function getUserRole() { return localStorage.getItem('user_role') || ''; }
function getUserId() { return parseInt(localStorage.getItem('user_id')) || 0; }
function getUserName() { return localStorage.getItem('user_name') || ''; }

function requireAuth() {
  const token = api.getToken();
  if (!token) {
    const path = window.location.pathname;
    if (path.includes('/admin/') || path.includes('pages/admin')) goTo('/pages/admin/login.html');
    else goTo('/login.html');
    return false;
  }
  return true;
}

function requireRole(role) {
  if (!requireAuth()) return false;
  const r = getUserRole();
  if (r !== role) {
    if (r === 'admin') goTo('/pages/admin/dashboard.html');
    else goTo(`/pages/${r}/dashboard.html`);
    return false;
  }
  return true;
}

function redirectToLogin() {
  const role = getUserRole();
  if (role === 'admin') { goTo('/pages/admin/login.html'); return true; }
  goTo('/login.html');
  return false;
}

function redirectToDashboard() {
  const role = getUserRole();
  if (role) { goTo(`/pages/${role}/dashboard.html`); return true; }
  return false;
}

function logout() {
  const role = getUserRole();
  clearSession();
  if (role === 'admin') goTo('/pages/admin/login.html');
  else goTo('/login.html');
}

function setupNavbar() {
  const nav = document.querySelector('.navbar-nav');
  if (!nav) return;
  const role = getUserRole();
  const name = getUserName();
  const userHtml = `
    <li class="navbar-user">
      <div class="avatar">${name ? name.charAt(0).toUpperCase() : 'U'}</div>
      <span style="color:var(--text-muted);font-size:0.85rem">${name || ''}</span>
      <span class="user-badge">${role || ''}</span>
      <button onclick="logout()" class="btn btn-ghost btn-sm">Logout</button>
    </li>`;
  nav.innerHTML = nav.innerHTML.replace('<!-- USER -->', userHtml);
}

function initPage() {
  if (!requireAuth()) return;
  setupNavbar();
}
