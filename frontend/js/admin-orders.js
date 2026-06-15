(function () {
  window.HomemadeCookieAuth.requireAdmin();

  const loading = document.getElementById('orders-loading');
  const errorEl = document.getElementById('orders-error');
  const listEl = document.getElementById('orders-list');
  const messageEl = document.getElementById('orders-message');
  const tabs = document.querySelectorAll('.status-tab');

  let allOrders = [];
  let selectedStatus = 'All';

  function formatMoney(value) {
    return `RM ${Number(value).toFixed(2)}`;
  }

  function showMessage(text, type) {
    messageEl.hidden = false;
    messageEl.className = `result ${type}`;
    messageEl.textContent = text;
  }

  async function advanceOrder(orderId) {
    try {
      const result = await window.HomemadeCookieApi.advanceOrder(orderId);
      showMessage(`Order #${orderId}: ${result.statusName}`, 'success');
      await loadOrders();
    } catch (error) {
      showMessage(error.message, 'error');
    }
  }

  async function viewDetails(orderId) {
    try {
      const data = await window.HomemadeCookieApi.getAdminOrderDetails(orderId);
      const box = document.getElementById(`admin-detail-${orderId}`);
      const reviews = await window.HomemadeCookieApi.getOrderReviews(orderId);

      box.hidden = !box.hidden;

      if (box.hidden) return;

      box.innerHTML = `
        <div class="detail-card">
          <h3>Order #${data.orderId}</h3>
          <p><strong>Customer:</strong> ${data.customerName}</p>
          <p><strong>Phone:</strong> ${data.phoneNumber}</p>
          <p><strong>Address:</strong> ${data.address || 'No address provided'}</p>
          <p><strong>Ordered date:</strong> ${new Date(data.orderDate).toLocaleString()}</p>
          <p><strong>Status:</strong> ${data.statusName}</p>

          <h4>Ordered cookies</h4>
          <table class="detail-table">
            <thead>
              <tr>
                <th>Cookie flavor</th>
                <th>Quantity</th>
                <th>Price at purchase</th>
                <th>Subtotal</th>
              </tr>
            </thead>
            <tbody>
              ${data.items.map(i => `
                <tr>
                  <td>${i.cookieName}</td>
                  <td>${i.quantity}</td>
                  <td>${formatMoney(i.priceAtPurchase)}</td>
                  <td>${formatMoney(i.quantity * i.priceAtPurchase)}</td>
                </tr>
              `).join('')}
            </tbody>
          </table>

          <p class="total-line"><strong>Total price:</strong> ${formatMoney(data.totalAmount)}</p>

          <h4>Customer reviews</h4>
          <div class="review-list">
            ${reviews.length ? reviews.map(r => {
              const cookieName = r.cookieName || r.cookie_name || 'Unknown cookie';
              const customerName = r.customerName || r.customer_name || 'Unknown customer';

              return `
                <div class="admin-review-card">
                  <p><strong>${cookieName}</strong> — ${'★'.repeat(r.rating)}${'☆'.repeat(5 - r.rating)}</p>
                  <p>${r.comment || 'No comment provided.'}</p>
                  <small>By ${customerName} · ${new Date(r.createdAt || r.created_at).toLocaleString()}</small>
                </div>
              `;
            }).join('') : '<p class="empty-state">No review received yet.</p>'}
          </div>
        </div>
      `;
    } catch (error) {
      showMessage(error.message, 'error');
    }
  }

  function renderOrders() {
    const orders = selectedStatus === 'All'
      ? allOrders
      : allOrders.filter(o => o.statusName === selectedStatus);

    if (!orders.length) {
      listEl.innerHTML = `<p class="empty-state">No ${selectedStatus} orders found.</p>`;
      return;
    }

    listEl.innerHTML = orders.map((o) => `
      <article class="order-card">
        <header>
          <strong>Order #${o.orderId}</strong>
          <span class="order-status status-${o.statusName.toLowerCase()}">${o.statusName}</span>
        </header>

        <p>${new Date(o.orderDate).toLocaleString()} · ${formatMoney(o.totalAmount)}</p>

        <button type="button" class="btn-view-detail" data-id="${o.orderId}">
          View order details
        </button>

        <button type="button" class="btn-secondary btn-advance" data-id="${o.orderId}" ${o.statusId === 1 ? 'disabled' : ''}>
          ${o.statusId === 1 ? 'Awaiting Payment' : 'Advance status'}
        </button>

        <div id="admin-detail-${o.orderId}" class="admin-order-detail" hidden></div>
      </article>
    `).join('');

    listEl.querySelectorAll('.btn-advance').forEach((btn) => {
      btn.addEventListener('click', () => advanceOrder(Number(btn.dataset.id)));
    });

    listEl.querySelectorAll('.btn-view-detail').forEach((btn) => {
      btn.addEventListener('click', () => viewDetails(Number(btn.dataset.id)));
    });
  }

  tabs.forEach((tab) => {
    tab.addEventListener('click', () => {
      tabs.forEach(t => t.classList.remove('active'));
      tab.classList.add('active');
      selectedStatus = tab.dataset.status;
      renderOrders();
    });
  });

  async function loadOrders() {
    loading.hidden = false;
    errorEl.hidden = true;
    listEl.hidden = true;
    messageEl.hidden = true;

    try {
      allOrders = await window.HomemadeCookieApi.getAdminOrders();
      loading.hidden = true;
      listEl.hidden = false;
      renderOrders();
    } catch (error) {
      loading.hidden = true;
      errorEl.hidden = false;
      errorEl.textContent = error.message;
    }
  }

  loadOrders();
})();