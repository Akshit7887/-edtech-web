let googleClientId = '';

let _gisFallbackTimer = null;

function showFallbackButton() {
  const fallback = document.getElementById('google-fallback-btn');
  if (fallback) fallback.style.display = 'flex';
}

async function initGoogleAuth(buttonContainerId = 'google-signin-container') {
  _gisFallbackTimer = setTimeout(showFallbackButton, 5000);
  try {
    const res = await fetch(`${API_BASE}/auth/google/config`);
    if (!res.ok) throw new Error('Failed to fetch Google config');
    const config = await res.json();
    googleClientId = config.clientId;
    if (!googleClientId) {
      console.warn('[Google Auth] No Google Client ID configured');
      showFallbackButton();
      return;
    }
    if (typeof google === 'undefined' || !google.accounts) {
      console.warn('[Google Auth] GIS library not loaded yet, retrying...');
      setTimeout(() => initGoogleAuth(buttonContainerId), 500);
      return;
    }
    google.accounts.id.initialize({
      client_id: googleClientId,
      callback: handleCredentialResponse,
      cancel_on_tap_outside: false,
    });
    const container = document.getElementById(buttonContainerId);
    if (container) {
      google.accounts.id.renderButton(container, {
        theme: 'outline',
        size: 'large',
        width: container.offsetWidth || 300,
        text: 'signin_with',
        shape: 'rectangular',
      });
    }
    google.accounts.id.prompt();
    clearTimeout(_gisFallbackTimer);
  } catch (e) {
    console.error('[Google Auth] Init error:', e);
    showFallbackButton();
  }
}

async function handleCredentialResponse(response) {
  if (!response || !response.credential) {
    showAlert('Google sign-in failed. Please try again.', 'error', document.getElementById('alert-container'));
    return;
  }
  try {
    const role = typeof selectedRole !== 'undefined' ? selectedRole : 'student';
    const res = await fetch(`${API_BASE}/auth/google/signin`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ id_token: response.credential, role }),
    });
    if (!res.ok) {
      const err = await res.json().catch(() => ({ error: 'Google sign-in failed' }));
      throw new Error(err.error || err.message || 'Google sign-in failed');
    }
    const data = await res.json();
    saveSession(data.token, {
      id: data.user.id,
      name: data.user.name,
      role: data.user.role,
      email: data.user.email,
      phone: data.user.phone || '',
    });
    redirectToDashboard();
  } catch (e) {
    showAlert(e.message, 'error', document.getElementById('alert-container'));
  }
}

function signInWithGoogle(role) {
  if (googleClientId && typeof google !== 'undefined' && google.accounts) {
    google.accounts.id.initialize({
      client_id: googleClientId,
      callback: handleCredentialResponse,
    });
    google.accounts.id.prompt();
  } else {
    window.location.href = API_BASE + '/auth/google/login?role=' + role;
  }
}

function handleGoogleCallback() {
  const params = new URLSearchParams(window.location.search);

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
      showAlert('Authentication incomplete (missing role). Please try again.', 'error', document.getElementById('alert-container'));
      setTimeout(() => goTo('login.html'), 3000);
      return;
    }

    saveSession(token, { id: parseInt(userId), name, role, email, phone: '' });
    redirectToDashboard();
    return;
  }

  showAlert('No sign-in data received. Redirecting to login...', 'error', document.getElementById('alert-container'));
  setTimeout(() => goTo('login.html'), 3000);
}
