// Google OAuth via Backend
const GOOGLE_AUTH_URL = `${typeof API_BASE !== 'undefined' ? API_BASE.replace('/api', '') : 'http://localhost:5000'}/auth/google/login`;

function signInWithGoogle() {
  window.location.href = GOOGLE_AUTH_URL;
}

function handleGoogleCallback() {
  const params = new URLSearchParams(window.location.search);
  const token = params.get('token');
  const error = params.get('error');

  if (error) {
    showAlert(error || 'Google sign-in failed. Please try again.', 'error');
    setTimeout(() => goTo('login.html'), 2000);
    return;
  }

  if (token) {
    const userId = params.get('user_id');
    const name = params.get('name');
    const role = params.get('role');
    const email = params.get('email');

    saveSession(token, { id: parseInt(userId), name, role, email, phone: '' });
    redirectToDashboard();
  }
}
