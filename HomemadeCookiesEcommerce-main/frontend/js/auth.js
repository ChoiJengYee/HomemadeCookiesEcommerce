(function () {
  const DEMO_ACCOUNTS = {
    customer: {
      email: 'customer@homemadecookies.com',
      password: 'customer123'
    },
    admin: {
      email: 'admin@homemadecookies.com',
      password: 'admin123'
    }
  };

  let currentUser = null;

  // -----------------------------
  // Helpers
  // -----------------------------
  function normalizeRole(role) {
    return (role || '').toLowerCase().trim();
  }

  function normalizeUser(user) {
    if (!user) return null;

    return {
      ...user,
      userId: user.userId ?? user.UserId,
      name: user.name ?? user.Name,
      email: user.email ?? user.Email,
      role: user.role ?? user.Role,
      address: user.address ?? user.Address,
      phoneNumber: user.phoneNumber ?? user.PhoneNumber
    };
  }

  // -----------------------------
  // State management
  // -----------------------------
  function saveUser(user) {
    currentUser = normalizeUser(user);

    if (currentUser) {
      sessionStorage.setItem('hc_user', JSON.stringify(currentUser));
    } else {
      sessionStorage.removeItem('hc_user');
    }

    updateNav();
  }

  function loadStoredUser() {
    try {
      const raw = sessionStorage.getItem('hc_user');
      if (raw) currentUser = normalizeUser(JSON.parse(raw));
    } catch {
      currentUser = null;
    }
  }

  // -----------------------------
  // UI
  // -----------------------------
  function updateNav() {
    const userEl = document.getElementById('nav-user');
    if (!userEl) return;

    const adminLinks = document.querySelectorAll('[data-admin-only]');
    const customerLinks = document.querySelectorAll('[data-customer-only]');

    if (currentUser) {
      const roleLabel = currentUser.role ? ` (${currentUser.role})` : '';
      const isAdmin = normalizeRole(currentUser.role) === 'admin';

      const profileLink = isAdmin
        ? '/admin/dashboard.html'
        : '/dashboard.html';

      userEl.innerHTML = `
        <a href="${profileLink}" class="btn-link" aria-label="Profile">👤</a>
        <span class="nav-user-name">${currentUser.name}${roleLabel}</span>
        <button type="button" class="btn-link" id="btn-logout">Logout</button>
      `;

      userEl.querySelector('#btn-logout')?.addEventListener('click', async () => {
        await logout();
        window.location.href = '/login.html';
      });
    } else {
      userEl.innerHTML = `<a href="/login.html" class="btn-link">👤 Login</a>`;
    }

    adminLinks.forEach(el => {
      el.hidden = !currentUser || normalizeRole(currentUser.role) !== 'admin';
    });

    customerLinks.forEach(el => {
      el.hidden = !currentUser || normalizeRole(currentUser.role) !== 'customer';
    });
  }

  // -----------------------------
  // Server sync (manual only)
  // -----------------------------
  async function refreshFromServer() {
    try {
      const data = await window.HomemadeCookieApi.getMe();

      if (data?.authenticated && data?.user) {
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

  // -----------------------------
  // Auth guards (IMPORTANT FIX)
  // -----------------------------
  async function requireRole(role, redirectTo = '/login.html') {
    const user = currentUser; // NO auto refresh (fixes instability)

    if (!user || normalizeRole(user.role) !== normalizeRole(role)) {
      const next = encodeURIComponent(
        window.location.pathname + window.location.search
      );

      window.location.href = `${redirectTo}?next=${next}`;
      return null;
    }

    return user;
  }

  async function requireCustomer() {
    return requireRole('customer');
  }

  async function requireAdmin() {
    return requireRole('admin');
  }

  // -----------------------------
  // API helpers
  // -----------------------------
  function getCustomerId() {
    return normalizeRole(currentUser?.role) === 'customer'
      ? currentUser.userId
      : null;
  }

  function getUser() {
    return currentUser;
  }

  async function login(email, password) {
    const data = await window.HomemadeCookieApi.login(email, password);

    const user = data?.user ?? data;
    saveUser(user);

    return data;
  }

  async function logout() {
    try {
      await window.HomemadeCookieApi.logout();
    } catch {
      // ignore API errors
    }

    saveUser(null);
  }

  // -----------------------------
  // Init
  // -----------------------------
  loadStoredUser();
  updateNav();
  refreshFromServer();

  // -----------------------------
  // Public API
  // -----------------------------
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