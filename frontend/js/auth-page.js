(function () {
  const loginForm = document.getElementById('login-form');
  const registerForm = document.getElementById('register-form');
  const loginResult = document.getElementById('login-result');
  const registerResult = document.getElementById('register-result');
  const registerCard = document.getElementById('register-card');
  const roleHint = document.getElementById('roleHint');

  const params = new URLSearchParams(window.location.search);
  const nextUrl = params.get('next') || '/index.html';

  roleHint?.addEventListener('change', () => {
    const key = roleHint.value;
    if (!key || !window.HomemadeCookieAuth.DEMO_ACCOUNTS[key]) return;
    const acc = window.HomemadeCookieAuth.DEMO_ACCOUNTS[key];
    document.getElementById('email').value = acc.email;
    document.getElementById('password').value = acc.password;
  });

  document.getElementById('show-register')?.addEventListener('click', (e) => {
    e.preventDefault();
    registerCard.hidden = false;
    loginForm.closest('.card').hidden = true;
  });

  document.getElementById('show-login')?.addEventListener('click', (e) => {
    e.preventDefault();
    registerCard.hidden = true;
    loginForm.closest('.card').hidden = false;
  });

  function redirectAfterLogin(user) {
    if (user.role === 'Admin') {
      window.location.href = params.get('next')?.includes('admin') ? nextUrl : '/admin/orders.html';
      return;
    }
    window.location.href = nextUrl;
  }

  loginForm?.addEventListener('submit', async (e) => {
    e.preventDefault();
    loginResult.hidden = false;
    loginResult.className = 'result';
    loginResult.textContent = 'Signing in…';

    try {
      const user = await window.HomemadeCookieAuth.login(
        document.getElementById('email').value.trim(),
        document.getElementById('password').value
      );
      loginResult.className = 'result success';
      loginResult.textContent = `Welcome, ${user.name}!`;
      redirectAfterLogin(user);
    } catch (err) {
      loginResult.className = 'result error';
      loginResult.textContent = err.message;
    }
  });

  registerForm?.addEventListener('submit', async (e) => {
    e.preventDefault();
    registerResult.hidden = false;
    registerResult.className = 'result';
    registerResult.textContent = 'Creating account…';

    try {
      const user = await window.HomemadeCookieApi.register({
        name: document.getElementById('regName').value.trim(),
        email: document.getElementById('regEmail').value.trim(),
        password: document.getElementById('regPassword').value,
        address: document.getElementById('regAddress').value.trim(),
        phoneNumber: document.getElementById('regPhone').value.trim()
      });
      registerResult.className = 'result success';
      registerResult.textContent = `Registered as ${user.name}. Redirecting…`;
      window.location.href = '/index.html';
    } catch (err) {
      registerResult.className = 'result error';
      registerResult.textContent = err.message;
    }
  });

  window.HomemadeCookieApi.getMe().then((data) => {
    if (data.authenticated && data.user) redirectAfterLogin(data.user);
  }).catch(() => {});
})();
