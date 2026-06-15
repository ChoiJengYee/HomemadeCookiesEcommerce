const API_BASE = `${window.location.origin}/api`;

function normalizeProducts(data) {
  if (Array.isArray(data)) return data;
  if (data && Array.isArray(data.value)) return data.value;
  return [];
}

async function apiRequest(path, options = {}) {
  const url = `${API_BASE}${path}`;

  const response = await fetch(url, {
    credentials: 'include',
    headers: {
      'Content-Type': 'application/json',
      ...(options.headers || {})
    },
    ...options
  });

  const text = await response.text();

  let data = null;
  if (text) {
    try {
      data = JSON.parse(text);
    } catch {
      data = { message: text };
    }
  }

  if (!response.ok) {
    const message = data?.message || data?.title || `Request failed (${response.status})`;
    const err = new Error(message);
    err.status = response.status;
    throw err;
  }

  return data;
}

function getCustomerId() {
  return window.HomemadeCookieAuth?.getCustomerId?.() ?? null;
}

function resolveWishlistPath(customerId, suffix = '') {
  return customerId ? `/wishlist/${customerId}${suffix}` : `/wishlist${suffix}`;
}

window.HomemadeCookieApi = {

  // ========================
  // CORE UTIL
  // ========================
  normalizeProducts,
  apiRequest,
  getCustomerId,

  // ========================
  // AUTH
  // ========================
  getMe() {
    return apiRequest('/auth/me');
  },

  login(email, password) {
    return apiRequest('/auth/login', {
      method: 'POST',
      body: JSON.stringify({ email, password })
    }).then(r => r.user);
  },

  logout() {
    return apiRequest('/auth/logout', { method: 'POST' });
  },

  register(payload) {
    return apiRequest('/auth/register', {
      method: 'POST',
      body: JSON.stringify(payload)
    }).then(r => r.user);
  },

  updateProfile(payload) {
    return apiRequest('/auth/profile', {
      method: 'PUT',
      body: JSON.stringify(payload)
    }).then(r => r.user);
  },

  forgotPassword(payload) {
    return apiRequest('/auth/forgot-password', {
      method: 'POST',
      body: JSON.stringify(payload)
    });
  },

  // ========================
  // PRODUCTS
  // ========================
  async getProducts() {
    const data = await apiRequest('/products');
    return normalizeProducts(data);
  },

  getProduct(id) {
    return apiRequest(`/products/${id}`);
  },

  // ========================
  // CART
  // ========================
  getCart() {
    const id = getCustomerId();
    if (!id) return Promise.reject(new Error('Please log in as a customer.'));
    return apiRequest(`/cart/${id}`);
  },

  addToCart(cookieId, quantity = 1, options = {}) {
    const id = getCustomerId();
    if (!id) return Promise.reject(new Error('Please log in as a customer.'));

    return apiRequest(`/cart/${id}/items`, {
      method: 'POST',
      body: JSON.stringify({
        cookieId,
        quantity,
        customPrice: options.customPrice
      })
    });
  },

  updateCartItem(cookieId, quantity) {
    const id = getCustomerId();
    if (!id) return Promise.reject(new Error('Please log in as a customer.'));

    return apiRequest(`/cart/${id}/items/${cookieId}`, {
      method: 'PUT',
      body: JSON.stringify({ quantity })
    });
  },

  removeFromCart(cookieId) {
    const id = getCustomerId();
    if (!id) return Promise.reject(new Error('Please log in as a customer.'));

    return apiRequest(`/cart/${id}/items/${cookieId}`, {
      method: 'DELETE'
    });
  },

  // ========================
  // WISHLIST
  // ========================
  getWishlist() {
    const id = getCustomerId();
    return apiRequest(resolveWishlistPath(id));
  },

  addToWishlist(cookieId) {
    const id = getCustomerId();
    return apiRequest(resolveWishlistPath(id, '/items'), {
      method: 'POST',
      body: JSON.stringify({ cookieId })
    });
  },

  removeFromWishlist(cookieId) {
    const id = getCustomerId();
    return apiRequest(resolveWishlistPath(id, `/items/${cookieId}`), {
      method: 'DELETE'
    });
  },

  moveWishlistToCart() {
    const id = getCustomerId();
    return apiRequest(resolveWishlistPath(id, '/move-to-cart'), {
      method: 'POST'
    });
  },

  // ========================
  // ORDERS (CUSTOMER)
  // ========================
  checkout(payload) {
    return apiRequest('/orders/checkout', {
      method: 'POST',
      body: JSON.stringify(payload)
    });
  },

  savePendingOrder(payload) {
    return apiRequest('/orders/save-pending', {
      method: 'POST',
      body: JSON.stringify(payload)
    });
  },

  getMyOrders() {
    const id = getCustomerId();
    if (!id) return Promise.reject(new Error('Please log in as a customer.'));
    return apiRequest(`/orders/customer/${id}`);
  },

  getOrderStatus(orderId) {
    return apiRequest(`/orders/${orderId}/status`);
  },

  cancelOrder(orderId) {
    return apiRequest(`/orders/${orderId}/cancel`, {
      method: 'POST'
    });
  },

  payPendingOrder(orderId, payload) {
    return apiRequest(`/orders/${orderId}/pay-pending`, {
      method: 'POST',
      body: JSON.stringify(payload)
    });
  },

  // ========================
  // ADMIN REPORTS
  // ========================
  getAdminOrders(startDate, endDate) {
    let url = '/admin/orders';

    const params = new URLSearchParams();

    if (startDate) params.append('startDate', startDate);
    if (endDate) params.append('endDate', endDate);

    const query = params.toString();
    if (query) url += `?${query}`;

    return apiRequest(url);
  },

  getAdminOrderDetails(orderId) {
    return apiRequest(`/admin/orders/${orderId}/details`);
  },

  advanceOrder(orderId) {
    return apiRequest(`/admin/orders/${orderId}/advance`, {
      method: 'PUT'
    });
  },

  // ========================
  // ADMIN DASHBOARD STATS
  // ========================
  getAdminStats() {
    return apiRequest('/admin/dashboard/stats');
  },

  getAdminSalesData(startDate, endDate) {
    let url = '/admin/sales';
    const params = new URLSearchParams();
    if (startDate) params.append('startDate', startDate);
    if (endDate) params.append('endDate', endDate);
    const query = params.toString();
    if (query) url += `?${query}`;
    return apiRequest(url);
  },

  // ========================
  // CATEGORIES (Admin)
  // ========================
  getCategories() {
    return apiRequest('/admin/categories');
  },

  // Alias for getCategories - use this for consistency
  getAdminCategories() {
    return this.getCategories();
  },

  createCategory(payload) {
    return apiRequest('/admin/categories', {
      method: 'POST',
      body: JSON.stringify(payload)
    });
  },

  // Alias for createCategory
  createAdminCategory(payload) {
    return this.createCategory(payload);
  },

  updateCategory(id, payload) {
    return apiRequest(`/admin/categories/${id}`, {
      method: 'PUT',
      body: JSON.stringify(payload)
    });
  },

  // Alias for updateCategory
  updateAdminCategory(id, payload) {
    return this.updateCategory(id, payload);
  },

  deleteCategory(id) {
    return apiRequest(`/admin/categories/${id}`, {
      method: 'DELETE'
    });
  },

  // Alias for deleteCategory
  deleteAdminCategory(id) {
    return this.deleteCategory(id);
  },

  // ========================
  // COOKIES / PRODUCTS (Admin)
  // ========================
  async createCookie(formData) {
    const res = await fetch(`${API_BASE}/admin/cookies`, {
      method: 'POST',
      credentials: 'include',
      body: formData
    });

    if (!res.ok) throw new Error(await res.text());
    return res.json();
  },

  async updateCookie(cookieId, formData) {
    const res = await fetch(`${API_BASE}/admin/cookies/${cookieId}`, {
      method: 'PUT',
      credentials: 'include',
      body: formData
    });

    const text = await res.text();

    if (!res.ok) {
      throw new Error(text || `Request failed (${res.status})`);
    }

    return text ? JSON.parse(text) : null;
  },

  deleteCookie(cookieId) {
    return apiRequest(`/admin/cookies/${cookieId}`, {
      method: 'DELETE'
    });
  },

  // Get all products (admin view)
  getAdminProducts() {
    return apiRequest('/admin/products');
  },

  // Get single product for admin
  getAdminProduct(id) {
    return apiRequest(`/admin/products/${id}`);
  },

  // Create product with JSON (if needed)
  createAdminProduct(payload) {
    return apiRequest('/admin/products', {
      method: 'POST',
      body: JSON.stringify(payload)
    });
  },

  // Update product with JSON (if needed)
  updateAdminProduct(id, payload) {
    return apiRequest(`/admin/products/${id}`, {
      method: 'PUT',
      body: JSON.stringify(payload)
    });
  },

  // ========================
  // REVIEWS (Admin)
  // ========================
  createReview(payload) {
    return apiRequest('/reviews', {
      method: 'POST',
      body: JSON.stringify(payload)
    });
  },

  getOrderReviews(orderId) {
    return apiRequest(`/reviews/order/${orderId}`);
  },

  getAdminReviews() {
    return apiRequest('/admin/reviews');
  },

  deleteAdminReview(reviewId) {
    return apiRequest(`/admin/reviews/${reviewId}`, {
      method: 'DELETE'
    });
  },

  // ========================
  // USERS (Admin)
  // ========================
  getAdminUsers() {
    return apiRequest('/admin/users');
  },

  getUserById(userId) {
    return apiRequest(`/admin/users/${userId}`);
  },

  updateUserRole(userId, payload) {
    return apiRequest(`/admin/users/${userId}/role`, {
      method: 'PUT',
      body: JSON.stringify(payload)
    });
  },

  updateUserStatus(userId, payload) {
    return apiRequest(`/admin/users/${userId}/status`, {
      method: 'PUT',
      body: JSON.stringify(payload)
    });
  },

  deleteAdminUser(userId) {
    return apiRequest(`/admin/users/${userId}`, {
      method: 'DELETE'
    });
  },

  // ========================
  // DISCOUNTS / PROMOTIONS (Admin)
  // ========================
  getAdminDiscounts() {
    return apiRequest('/admin/discounts');
  },

  createDiscount(payload) {
    return apiRequest('/admin/discounts', {
      method: 'POST',
      body: JSON.stringify(payload)
    });
  },

  updateDiscount(id, payload) {
    return apiRequest(`/admin/discounts/${id}`, {
      method: 'PUT',
      body: JSON.stringify(payload)
    });
  },

  deleteDiscount(id) {
    return apiRequest(`/admin/discounts/${id}`, {
      method: 'DELETE'
    });
  },

  // ========================
  // SETTINGS (Admin)
  // ========================
  getAdminSettings() {
    return apiRequest('/admin/settings');
  },

  updateAdminSettings(payload) {
    return apiRequest('/admin/settings', {
      method: 'PUT',
      body: JSON.stringify(payload)
    });
  }
};