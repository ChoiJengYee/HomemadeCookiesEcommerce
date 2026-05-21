(function () {
  const DEMO_ACCOUNTS = {
    customer: { email: 'customer@homemadecookies.com', password: 'customer123' },
    admin: { email: 'admin@homemadecookies.com', password: 'admin123' }
  };

  let currentUser = null;

  function saveUser(user) {
    currentUser = user;
    if (user) {
      sessionStorage.setItem('hc_user', JSON.stringify(user));
    } else {
      sessionStorage.removeItem('hc_user');
    }
    updateNav();
  }

  function loadStoredUser() {
    try {
      const raw = sessionStorage.getItem('hc_user');
      if (raw) currentUser = JSON.parse(raw);
    } catch {
      currentUser = null;
    }
  }

  function updateNav() {
    const userEl = document.getElementById('nav-user');
    const adminLinks = document.querySelectorAll('[data-admin-only]');
    const customerLinks = document.querySelectorAll('[data-customer-only]');

    if (userEl) {
      if (currentUser) {
        userEl.innerHTML = `
          <span class="nav-user-name">${currentUser.name} (${currentUser.role})</span>
          <button type="button" class="btn-link" id="btn-logout">Logout</button>
        `;
        userEl.querySelector('#btn-logout')?.addEventListener('click', () => {
          window.HomemadeCookieAuth.logout().then(() => {
            window.location.href = '/login.html';
          });
        });
      } else {
        userEl.innerHTML = '<a href="/login.html">Login</a>';
      }
    }

    adminLinks.forEach((el) => {
      el.hidden = !currentUser || currentUser.role !== 'Admin';
    });

    customerLinks.forEach((el) => {
      el.hidden = !currentUser || currentUser.role !== 'Customer';
    });
  }

  async function refreshFromServer() {
    try {
      const data = await window.HomemadeCookieApi.getMe();
      if (data.authenticated && data.user) {
        saveUser(data.user);
        return data.user;
      }
      saveUser(null);
      return null;
    } catch {
      saveUser(null);
      return null;
    }
  }

  async function requireRole(role, redirectTo = '/login.html') {
    const user = await refreshFromServer();
    if (!user || user.role !== role) {
      const next = encodeURIComponent(window.location.pathname + window.location.search);
      window.location.href = `${redirectTo}?next=${next}`;
      return null;
    }
    return user;
  }

  async function requireCustomer() {
    return requireRole('Customer');
  }

  async function requireAdmin() {
    return requireRole('Admin');
  }

  function getCustomerId() {
    return currentUser?.role === 'Customer' ? currentUser.userId : null;
  }

  function getUser() {
    return currentUser;
  }

  async function login(email, password) {
    const data = await window.HomemadeCookieApi.login(email, password);
    saveUser(data);
    return data;
  }

  async function logout() {
    try {
      await window.HomemadeCookieApi.logout();
    } catch {
      /* ignore */
    }
    saveUser(null);
  }

  loadStoredUser();
  updateNav();
  refreshFromServer();

  window.HomemadeCookieAuth = {
    DEMO_ACCOUNTS,
    getUser,
    getCustomerId,
    requireCustomer,
    requireAdmin,
    requireRole,
    login,
    logout,
    refreshFromServer,
    updateNav
  };
})();
