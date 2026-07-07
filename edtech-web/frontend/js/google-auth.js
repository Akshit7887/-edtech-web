const GOOGLE_AUTH_URL = API_BASE + '/auth/google/login';

function signInWithGoogle(role) {
  window.location.href = GOOGLE_AUTH_URL + '?role=' + role;
}

function handleGoogleCallback() {
  const params = new URLSearchParams(window.location.search);
  console.log('[Google Auth] Full URL:', window.location.href);
  console.log('[Google Auth] Query params:', Object.fromEntries(params));

  const token = params.get('token');
  const error = params.get('error');

  if (error) {
    showAlert(error || 'Google sign-in failed. Please try again.', 'error', document.getElementById('alert-container'));
    setTimeout(() => goTo('login.html'), 3000);
    return;
  }

  if (token) {
    const userId = params.get('user_id');
    const name = params.get('name');
    const role = params.get('role');
    const email = params.get('email');

    if (!role) {
      console.error('[Google Auth] Missing role in params');
      showAlert('Authentication incomplete (missing role). Please try again.', 'error', document.getElementById('alert-container'));
      setTimeout(() => goTo('login.html'), 3000);
      return;
    }

    saveSession(token, { id: parseInt(userId), name, role, email, phone: '' });
    console.log('[Google Auth] Session saved, redirecting to dashboard as', role);
    redirectToDashboard();
    return;
  }

  console.error('[Google Auth] No token or error in URL');
  showAlert('No sign-in data received. Redirecting to login...', 'error', document.getElementById('alert-container'));
  setTimeout(() => goTo('login.html'), 3000);
}
