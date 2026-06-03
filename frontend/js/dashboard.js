(async function () {
  const user = await window.HomemadeCookieAuth.refreshFromServer();
  if (!user) {
    document.getElementById('dashboard-greeting').textContent =
      'Browse our cookies as a guest. Login when you are ready to add items to cart and wishlist.';

    const dashboardLinks = document.getElementById('dashboard-links');
    dashboardLinks.innerHTML = `
      <a class="dashboard-card-link" href="/index.html">🍪 Browse Cookies</a>
      <a class="dashboard-card-link" href="/login.html">🔐 Login</a>
    `;
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
