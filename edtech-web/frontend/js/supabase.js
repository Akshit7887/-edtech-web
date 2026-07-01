const GOOGLE_AUTH_URL = API_BASE + '/auth/google/login';

function signInWithGoogle(role) {
  window.location.href = GOOGLE_AUTH_URL + '?role=' + role;
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
