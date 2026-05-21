const API_BASE = `${window.location.origin}/api`;

/** Demo customer from seed.sql (user_id = 2) */
const DEMO_CUSTOMER_ID = 2;

function normalizeProducts(data) {
  if (Array.isArray(data)) return data;
  if (data && Array.isArray(data.value)) return data.value;
  return [];
}

async function apiRequest(path, options = {}) {
  const url = `${API_BASE}${path}`;
  const response = await fetch(url, {
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
    throw new Error(message);
  }

  return data;
}

window.HomemadeCookieApi = {
  DEMO_CUSTOMER_ID,
  normalizeProducts,

  async getProducts() {
    const data = await apiRequest('/products');
    return normalizeProducts(data);
  },

  getProduct(id) {
    return apiRequest(`/products/${id}`);
  },

  getCart(customerId = DEMO_CUSTOMER_ID) {
    return apiRequest(`/cart/${customerId}`);
  },

  addToCart(cookieId, quantity = 1, customerId = DEMO_CUSTOMER_ID) {
    return apiRequest(`/cart/${customerId}/items`, {
      method: 'POST',
      body: JSON.stringify({ cookieId, quantity })
    });
  },

  updateCartItem(cookieId, quantity, customerId = DEMO_CUSTOMER_ID) {
    return apiRequest(`/cart/${customerId}/items/${cookieId}`, {
      method: 'PUT',
      body: JSON.stringify({ quantity })
    });
  },

  removeFromCart(cookieId, customerId = DEMO_CUSTOMER_ID) {
    return apiRequest(`/cart/${customerId}/items/${cookieId}`, {
      method: 'DELETE'
    });
  },

  createCookie(payload) {
    return apiRequest('/admin/cookies', {
      method: 'POST',
      body: JSON.stringify(payload)
    });
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

  getAdminOrders() {
    return apiRequest('/admin/orders');
  },

  advanceOrder(orderId) {
    return apiRequest(`/admin/orders/${orderId}/advance`, { method: 'PUT' });
  }
};
