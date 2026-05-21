(function () {
  const loading = document.getElementById('orders-loading');
  const errorEl = document.getElementById('orders-error');
  const listEl = document.getElementById('orders-list');
  const messageEl = document.getElementById('orders-message');

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

  function renderOrders(orders) {
    if (!orders.length) {
      listEl.innerHTML = '<p class="empty-state">No orders yet. Place one from checkout.</p>';
      return;
    }

    listEl.innerHTML = orders.map((o) => `
      <article class="order-card" data-order-id="${o.orderId}">
        <header>
          <strong>Order #${o.orderId}</strong>
          <span class="order-status">${o.statusName}</span>
        </header>
        <p>${new Date(o.orderDate).toLocaleString()} · ${formatMoney(o.totalAmount)}</p>
        <button type="button" class="btn-primary btn-advance" data-id="${o.orderId}">Advance status</button>
      </article>
    `).join('');

    listEl.querySelectorAll('.btn-advance').forEach((btn) => {
      btn.addEventListener('click', () => advanceOrder(Number(btn.dataset.id)));
    });
  }

  async function loadOrders() {
    loading.hidden = false;
    errorEl.hidden = true;
    listEl.hidden = true;
    messageEl.hidden = true;

    try {
      const orders = await window.HomemadeCookieApi.getAdminOrders();
      loading.hidden = true;
      listEl.hidden = false;
      renderOrders(orders);
    } catch (error) {
      loading.hidden = true;
      errorEl.hidden = false;
      errorEl.textContent = error.message;
    }
  }

  loadOrders();
})();
