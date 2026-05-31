(async function () {
  const user = await window.HomemadeCookieAuth.refreshFromServer();
  if (!user) {
    const next = encodeURIComponent(window.location.pathname + window.location.search);
    window.location.href = `/login.html?next=${next}`;
    return;
  }

  if (user.role === 'Admin') {
    window.location.href = '/admin/dashboard.html';
    return;
  }

  document.getElementById('dashboard-greeting').textContent =
    `Hi ${user.name}! You can manage your profile, orders, wishlist, and checkout from here.`;

  const dashboardLinks = document.getElementById('dashboard-links');
  dashboardLinks.innerHTML = `
    <a class="dashboard-card-link" href="/cart.html">🛒 View Cart</a>
    <a class="dashboard-card-link" href="/wishlist.html">💖 Wishlist</a>
    <a class="dashboard-card-link" href="/orders.html">📦 My Orders</a>
    <a class="dashboard-card-link" href="/profile.html">👤 Edit Profile</a>
    <a class="dashboard-card-link" href="/checkout.html">💳 Checkout</a>
  `;
})();
