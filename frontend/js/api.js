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

window.HomemadeCookieApi = {
  normalizeProducts,
  getCustomerId,

  getMe() {
    return apiRequest('/auth/me');
  },

  login(email, password) {
    return apiRequest('/auth/login', {
      method: 'POST',
      body: JSON.stringify({ email, password })
    });
  },

  logout() {
    return apiRequest('/auth/logout', { method: 'POST' });
  },

  register(payload) {
    return apiRequest('/auth/register', {
      method: 'POST',
      body: JSON.stringify(payload)
    });
  },

  async getProducts() {
    const data = await apiRequest('/products');
    return normalizeProducts(data);
  },

  getProduct(id) {
    return apiRequest(`/products/${id}`);
  },

  getCart() {
    const id = getCustomerId();
    if (!id) return Promise.reject(new Error('Please log in as a customer.'));
    return apiRequest(`/cart/${id}`);
  },

  addToCart(cookieId, quantity = 1) {
    const id = getCustomerId();
    if (!id) return Promise.reject(new Error('Please log in as a customer.'));
    return apiRequest(`/cart/${id}/items`, {
      method: 'POST',
      body: JSON.stringify({ cookieId, quantity })
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
    return apiRequest(`/cart/${id}/items/${cookieId}`, { method: 'DELETE' });
  },

  getWishlist() {
    return apiRequest('/wishlist');
  },

  addToWishlist(cookieId) {
    return apiRequest('/wishlist/items', {
      method: 'POST',
      body: JSON.stringify({ cookieId })
    });
  },

  removeFromWishlist(cookieId) {
    return apiRequest(`/wishlist/items/${cookieId}`, { method: 'DELETE' });
  },

  moveWishlistToCart() {
    return apiRequest('/wishlist/move-to-cart', { method: 'POST' });
  },

  checkout(payload) {
    return apiRequest('/orders/checkout', {
      method: 'POST',
      body: JSON.stringify(payload)
    });
  },

  getOrderStatus(orderId) {
    return apiRequest(`/orders/${orderId}/status`);
  },

  cancelOrder(orderId) {
    return apiRequest(`/orders/${orderId}/cancel`, { method: 'POST' });
  },

  createCookie(payload) {
    return apiRequest('/admin/cookies', {
      method: 'POST',
      body: JSON.stringify(payload)
    });
  },

  getAdminOrders() {
    return apiRequest('/admin/orders');
  },

  advanceOrder(orderId) {
    return apiRequest(`/admin/orders/${orderId}/advance`, { method: 'PUT' });
  }
};
