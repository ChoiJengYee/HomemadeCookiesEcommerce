(function() {
  let sidebar = null;
  let overlay = null;
  let hamburgerBtn = null;

  async function initSidebar() {
    // Get user directly from auth
    let user = null;
    try {
      user = window.HomemadeCookieAuth.getUser();
      console.log("Initial user from storage:", user);
      
      await window.HomemadeCookieAuth.refreshFromServer();
      user = window.HomemadeCookieAuth.getUser();
    } catch(e) {
      console.error('Auth error:', e);
    }
    
    console.log("SIDEBAR FINAL USER:", user);
    
    const role = user?.role ? String(user.role).toLowerCase().trim() : null;
    const isAdmin = role === 'admin';
    const isCustomer = role === 'customer';
    const isLoggedIn = !!user;

    // Remove existing elements
    const existingSidebar = document.getElementById('app-sidebar');
    if (existingSidebar) existingSidebar.remove();
    const existingOverlay = document.getElementById('sidebar-overlay');
    if (existingOverlay) existingOverlay.remove();
    const existingHamburger = document.getElementById('hamburger-menu');
    if (existingHamburger) existingHamburger.remove();

    // Create hamburger button
    hamburgerBtn = document.createElement('button');
    hamburgerBtn.id = 'hamburger-menu';
    hamburgerBtn.className = 'hamburger-menu';
    hamburgerBtn.innerHTML = '☰';
    hamburgerBtn.setAttribute('aria-label', 'Toggle menu');
    document.body.appendChild(hamburgerBtn);

    // Create overlay
    overlay = document.createElement('div');
    overlay.id = 'sidebar-overlay';
    overlay.className = 'sidebar-overlay';
    document.body.appendChild(overlay);

    // Build sidebar HTML based on role
    let customerLinksHtml = '';
    let customerSectionHtml = '';
    let adminSectionHtml = '';

    if (isCustomer && !isAdmin) {
      customerLinksHtml = `
        <a href="/cart.html" class="nav-link">🛒 Cart</a>
        <a href="/wishlist.html" class="nav-link">💖 Wishlist</a>
        <a href="/track-order.html" class="nav-link">📦 Orders</a>
      `;
      customerSectionHtml = `
        <div class="sidebar-section">
          <h4>My Account</h4>
          <a href="/dashboard.html" class="nav-link">📊 Dashboard</a>
          <a href="/profile.html" class="nav-link">👤 Profile</a>
        </div>
      `;
    }

    if (isAdmin) {
      adminSectionHtml = `
        <div class="sidebar-section">
          <h4>Admin Panel</h4>
          <a href="/admin/dashboard.html" class="nav-link">📊 Admin Dashboard</a>
          <a href="/admin/products.html" class="nav-link">🍪 Manage Cookies</a>
          <a href="/admin/categories.html" class="nav-link">🗂 Categories</a>
          <a href="/admin/orders.html" class="nav-link">📦 Orders</a>
          <a href="/admin/users.html" class="nav-link">👥 Users</a>
        </div>
      `;
    }

    let authHtml = '';
    if (isLoggedIn) {
      const roleText = isAdmin ? 'Admin' : 'Customer';
      authHtml = `
        <div class="user-info">
          <span class="user-name">👤 ${user.name || user.email}</span>
          <span class="user-role">(${roleText})</span>
          <button class="btn-logout" id="sidebar-logout">🚪 Logout</button>
        </div>
      `;
    } else {
      authHtml = `
        <div class="user-info">
          <a href="/login.html" class="btn-login">🔐 Login</a>
          <a href="/register.html" class="btn-login" style="margin-top: 8px;">📝 Register</a>
        </div>
      `;
    }

    const sidebarHTML = `
      <aside class="sidebar" id="app-sidebar">
        <button class="sidebar-close" id="sidebar-close">✕</button>
        <div class="sidebar-header">
          🍪 <span>Homemade Cookies</span>
        </div>
        <nav class="sidebar-nav">
          <a href="/index.html" class="nav-link">🏠 Shop</a>
          ${customerLinksHtml}
          ${customerSectionHtml}
          ${adminSectionHtml}
          <div class="sidebar-footer">
            ${authHtml}
          </div>
        </nav>
      </aside>
    `;
    
    const tempDiv = document.createElement('div');
    tempDiv.innerHTML = sidebarHTML;
    sidebar = tempDiv.firstElementChild;
    document.body.insertBefore(sidebar, document.body.firstChild);
    
    // Hide original navigation
    const navElements = document.querySelectorAll('.site-nav, .site-header .site-nav, .professional-nav');
    navElements.forEach(el => {
      if (el) el.style.display = 'none';
    });
    
    // Setup logout button
    const logoutBtn = document.getElementById('sidebar-logout');
    if (logoutBtn) {
      logoutBtn.addEventListener('click', async () => {
        await window.HomemadeCookieAuth.logout();
        window.location.href = '/login.html';
      });
    }
    
    // Setup hamburger menu click
    hamburgerBtn.addEventListener('click', () => {
      sidebar.classList.toggle('open');
      overlay.classList.toggle('active');
      // Prevent body scroll when sidebar is open
      if (sidebar.classList.contains('open')) {
        document.body.style.overflow = 'hidden';
      } else {
        document.body.style.overflow = '';
      }
    });
    
    // Close sidebar when clicking overlay
    overlay.addEventListener('click', () => {
      sidebar.classList.remove('open');
      overlay.classList.remove('active');
      document.body.style.overflow = '';
    });
    
    // Close sidebar when clicking close button
    const closeBtn = document.getElementById('sidebar-close');
    if (closeBtn) {
      closeBtn.addEventListener('click', () => {
        sidebar.classList.remove('open');
        overlay.classList.remove('active');
        document.body.style.overflow = '';
      });
    }
    
    // Close sidebar when clicking a link (optional)
    const links = document.querySelectorAll('.sidebar-nav a');
    links.forEach(link => {
      link.addEventListener('click', () => {
        sidebar.classList.remove('open');
        overlay.classList.remove('active');
        document.body.style.overflow = '';
      });
    });
    
    // Sidebar starts hidden on all devices
    sidebar.classList.remove('open');
    overlay.classList.remove('active');
  }
  
  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', initSidebar);
  } else {
    initSidebar();
  }
})();