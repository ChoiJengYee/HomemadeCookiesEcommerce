(async function () {
  const {
    getProducts,
    getMyOrders,
    getOrderStatus,
    addToCart,
    addToWishlist,
    getCategories
  } = window.HomemadeCookieApi;

  const user = await window.HomemadeCookieAuth.refreshFromServer();

  const greeting = document.getElementById('dashboard-greeting');
  const dashboardLinks = document.getElementById('dashboard-links');

  const hotGrid = document.getElementById('hot-cookies-grid');
  const hotLoading = document.getElementById('hot-cookies-loading');
  const hotError = document.getElementById('hot-cookies-error');

  const orderAgainSection = document.getElementById('order-again-section');
  const orderAgainGrid = document.getElementById('order-again-grid');

  const toast = document.getElementById('dashboard-message');

  function formatPrice(amount) {
    return `RM ${Number(amount).toFixed(2)}`;
  }

  function escapeHtml(text) {
    const div = document.createElement('div');
    div.textContent = text ?? '';
    return div.innerHTML;
  }

  function showToast(message, isError = false) {
    toast.textContent = message;
    toast.className = `toast ${isError ? 'error' : 'success'}`;
    toast.hidden = false;

    clearTimeout(showToast.timer);
    showToast.timer = setTimeout(() => {
      toast.hidden = true;
    }, 3000);
  }

  function getCategoryName(categoryId) {
    if (Number(categoryId) === 1) return 'Best Seller';
    if (Number(categoryId) === 2) return 'Recommended';
    return '';
  }

  function renderCookieCards(products, container, limit = 4) {
    container.innerHTML = products.slice(0, limit).map((p) => {
      const outOfStock = p.stock <= 0;

      return `
        <article class="product-card dashboard-cookie-card">
          <span class="product-card-badge">${escapeHtml(getCategoryName(p.categoryId))}</span>

          <img 
            class="product-card-image" 
            src="${escapeHtml(p.imageUrl || '/images/cookie-default.svg')}" 
            alt="${escapeHtml(p.name)} image"
          >

          <h3>${escapeHtml(p.name)}</h3>
          <p class="product-desc">${escapeHtml(p.description || 'Freshly baked homemade cookies.')}</p>
          <p class="product-price">${formatPrice(p.price)}</p>

          <p class="product-stock ${outOfStock ? 'out' : ''}">
            ${outOfStock ? 'Out of stock' : `${p.stock} in stock`}
          </p>

          <div class="product-actions">
            <button 
              type="button" 
              class="btn-add dashboard-add-cart" 
              data-cookie-id="${p.cookieId}"
              ${outOfStock ? 'disabled' : ''}
            >
              ${outOfStock ? 'Unavailable' : 'Add to cart'}
            </button>

            <button 
              type="button" 
              class="btn-secondary btn-wish dashboard-add-wish" 
              data-cookie-id="${p.cookieId}"
            >
              ♥ Wishlist
            </button>
          </div>
        </article>
      `;
    }).join('');
  }

  async function loadHotCookies() {
    try {
      const products = await getProducts();
      const hotProducts = products
        .filter(p => p.stock > 0)
        .sort((a, b) => b.stock - a.stock)
        .slice(0, 4);

      hotLoading.hidden = true;
      renderCookieCards(hotProducts, hotGrid, 4);
    } catch (error) {
      hotLoading.hidden = true;
      hotError.hidden = false;
      hotError.textContent = error.message;
    }
  }

  async function loadOrderAgain() {
    try {
      const orders = await getMyOrders();

      if (!orders.length) return;

      const latestOrder = orders
        .slice()
        .sort((a, b) => new Date(b.orderDate) - new Date(a.orderDate))[0];

      const details = await getOrderStatus(latestOrder.orderId);
      const items = details.items || [];

      if (!items.length) return;

      const products = await getProducts();

      const previousCookies = items
        .map(item => products.find(p => p.cookieId === item.cookieId || p.name === item.cookieName))
        .filter(Boolean);

      if (!previousCookies.length) return;

      orderAgainSection.hidden = false;
      renderCookieCards(previousCookies, orderAgainGrid, 4);
    } catch {
      orderAgainSection.hidden = true;
    }
  }

  document.addEventListener('click', async (event) => {
    const cartBtn = event.target.closest('.dashboard-add-cart');
    const wishBtn = event.target.closest('.dashboard-add-wish');

    if (!cartBtn && !wishBtn) return;

    const button = cartBtn || wishBtn;
    const cookieId = Number(button.dataset.cookieId);

    button.disabled = true;

    try {
      if (cartBtn) {
        await addToCart(cookieId, 1);
        showToast('Added to cart!');
      }

      if (wishBtn) {
        await addToWishlist(cookieId);
        showToast('Added to wishlist!');
      }
    } catch (error) {
      showToast(error.message, true);
    } finally {
      button.disabled = false;
    }
  });

  if (!user) {
    greeting.textContent =
      'Browse our cookies as a guest. Login when you are ready to add items to cart and wishlist.';

    dashboardLinks.innerHTML = `
      <a class="dashboard-card-link" href="/index.html">🍪 Browse Cookies</a>
      <a class="dashboard-card-link" href="/login.html">🔐 Login</a>
    `;

    await loadHotCookies();
    return;
  }

  if (user.role === 'Admin') {
    window.location.href = '/admin/dashboard.html';
    return;
  }

  greeting.textContent =
    `Hi ${user.name}! You can manage your profile, orders, wishlist, and checkout from here.`;

  dashboardLinks.innerHTML = `
    <a class="dashboard-card-link" href="/cart.html">
      <span>🛒 View Cart</span>
      <small>Check selected cookies</small>
    </a>

    <a class="dashboard-card-link" href="/wishlist.html">
      <span>💖 Wishlist</span>
      <small>Saved favourites</small>
    </a>

    <a class="dashboard-card-link" href="/track-order.html">
      <span>📦 My Orders</span>
      <small>Track recent purchases</small>
    </a>

    <a class="dashboard-card-link" href="/profile.html">
      <span>👤 Edit Profile</span>
      <small>Update your details</small>
    </a>

    <a class="dashboard-card-link" href="/checkout.html">
      <span>💳 Checkout</span>
      <small>Complete payment</small>
    </a>
  `;

  await loadOrderAgain();
  await loadHotCookies();
})();