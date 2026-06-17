(function () {
  const loading = document.getElementById('orders-loading');
  const errorEl = document.getElementById('orders-error');
  const listEl = document.getElementById('customer-orders-list');
  const resultBox = document.getElementById('track-result');
  const detailBox = document.getElementById('order-detail');
  const tabs = document.querySelectorAll('.status-tab');

  let allOrders = [];
  let selectedFilter = 'All';
  let currentEmail = ''; // Store current user's email

  function formatMoney(value) {
    return `RM ${Number(value).toFixed(2)}`;
  }

  function isInProgress(status) {
    return ['Pending', 'Confirmed', 'Baking', 'Ready'].includes(status);
  }

  function getFilteredOrders() {
    if (selectedFilter === 'All') return allOrders;
    if (selectedFilter === 'InProgress') return allOrders.filter(o => isInProgress(o.statusName));
    if (selectedFilter === 'Achieved') return allOrders.filter(o => o.statusName === 'Completed');
    if (selectedFilter === 'Cancelled') return allOrders.filter(o => o.statusName === 'Cancelled');
    return allOrders;
  }

  function renderOrderList() {
    const orders = getFilteredOrders();

    resultBox.hidden = true;

    if (!orders.length) {
      listEl.innerHTML = '<p class="empty-state">No orders found.</p>';
      return;
    }

    listEl.innerHTML = orders.map((order) => `
      <article class="order-card">
        <header>
          <strong>Order #${order.orderId}</strong>
          <span class="order-status status-${order.statusName.toLowerCase()}">
            ${order.statusId === 1 ? 'Awaiting Payment' : order.statusName}
          </span>
        </header>

        <p>${new Date(order.orderDate).toLocaleString()} · ${formatMoney(order.totalAmount)}</p>

        <button type="button" class="btn-primary btn-track" data-id="${order.orderId}">
          View order details
        </button>

        <div id="customer-detail-${order.orderId}" class="inline-order-detail" hidden></div>
      </article>
    `).join('');

    listEl.querySelectorAll('.btn-track').forEach((btn) => {
      btn.addEventListener('click', () => loadStatus(Number(btn.dataset.id)));
    });
  }

  function renderOrderDetail(data) {
    const items = data.items || [];
    const canCancel = data.statusId === 1;

    document.querySelectorAll('.inline-order-detail').forEach(box => {
      box.hidden = true;
      box.innerHTML = '';
    });

    const detailBox = document.getElementById(`customer-detail-${data.orderId}`);
    detailBox.hidden = false;

    detailBox.innerHTML = `
      <div class="detail-card customer-detail-card">
        <div class="detail-header">
          <h3>Order #${data.orderId}</h3>
          <span class="order-status status-${data.statusName.toLowerCase()}">
            ${data.statusId === 1 ? 'Awaiting Payment' : data.statusName}
          </span>
        </div>

        <p><strong>Placed:</strong> ${new Date(data.orderDate).toLocaleString()}</p>
        <p><strong>Total:</strong> ${formatMoney(data.totalAmount)}</p>
        ${currentEmail ? `<p><strong>Email:</strong> ${currentEmail}</p>` : ''}

        <h4>Ordered cookies</h4>
        <ul class="detail-list">
          ${items.map((i) => `
            <li>
              <span>${i.cookieName}</span>
              <span>× ${i.quantity}</span>
              <span>${formatMoney(i.priceAtPurchase)}</span>
            </li>
          `).join('')}
        </ul>

        <button 
          type="button" 
          id="cancel-order-btn-${data.orderId}" 
          class="btn-danger-soft"
          data-order-id="${data.orderId}"
          ${canCancel ? '' : 'disabled'}>
          ${data.statusName === 'Cancelled' ? 'Order cancelled' : canCancel ? 'Cancel order' : 'Cannot cancel now'}
        </button>

        ${data.statusId === 1 ? `
          <button type="button" class="btn-primary pay-pending-btn" data-id="${data.orderId}">
            Pay now
          </button>
        ` : ''}

        ${data.statusId === 5 ? `
          <h4>Write a review</h4>

          ${items.map(i => `
            <div class="review-box">
              <p><strong>${i.cookieName}</strong></p>

              <select id="rating-${data.orderId}-${i.cookieId}">
                <option value="5">★★★★★ 5</option>
                <option value="4">★★★★ 4</option>
                <option value="3">★★★ 3</option>
                <option value="2">★★ 2</option>
                <option value="1">★ 1</option>
              </select>

              <textarea 
                id="comment-${data.orderId}-${i.cookieId}" 
                placeholder="Write your review..."
                rows="3"></textarea>

              <button 
                type="button" 
                class="btn-primary btn-submit-review"
                data-order-id="${data.orderId}"
                data-cookie-id="${i.cookieId}">
                Submit review
              </button>
            </div>
          `).join('')}
        ` : ''}
      </div>
    `;

    const cancelBtn = document.getElementById(`cancel-order-btn-${data.orderId}`);
    cancelBtn.addEventListener('click', () => cancelOrder(data.orderId));

    detailBox.querySelectorAll('.btn-submit-review').forEach((btn) => {
      btn.addEventListener('click', () => submitReview(
        Number(btn.dataset.orderId),
        Number(btn.dataset.cookieId),
        btn
      ));
    });

    document.querySelectorAll('.pay-pending-btn').forEach((btn) => {
      btn.addEventListener('click', () => {
        const orderId = btn.dataset.id;
        const emailParam = currentEmail ? `&email=${encodeURIComponent(currentEmail)}` : '';
        window.location.href = `/checkout.html?pendingOrderId=${orderId}${emailParam}`;
      });
    });
  }

  async function loadStatus(orderId) {
    resultBox.hidden = true;

    try {
      const data = await window.HomemadeCookieApi.getOrderStatus(orderId);
      renderOrderDetail(data);
    } catch (error) {
      resultBox.hidden = false;
      resultBox.className = 'result error';
      resultBox.textContent = error.message;
    }
  }

  async function cancelOrder(orderId) {
    const cancelBtn = document.getElementById(`cancel-order-btn-${orderId}`);
    if (!cancelBtn) return;
    
    // Confirm cancellation
    const confirmed = confirm('Are you sure you want to cancel this order?');
    if (!confirmed) return;

    cancelBtn.disabled = true;
    cancelBtn.textContent = 'Cancelling...';

    try {
      // Pass the email to the API
      const result = await window.HomemadeCookieApi.cancelOrder(orderId, currentEmail);

      resultBox.hidden = false;
      resultBox.className = 'result success';
      resultBox.textContent = result.message || 'Order cancelled successfully. A confirmation email has been sent.';

      // Refresh orders and reload details
      await loadCustomerOrders();
      await loadStatus(orderId);
    } catch (error) {
      resultBox.hidden = false;
      resultBox.className = 'result error';
      resultBox.textContent = error.message;
      cancelBtn.disabled = false;
      cancelBtn.textContent = 'Cancel order';
    }
  }

  async function loadCustomerOrders() {
    loading.hidden = false;
    errorEl.hidden = true;
    listEl.hidden = true;

    try {
      allOrders = await window.HomemadeCookieApi.getMyOrders();
      loading.hidden = true;
      listEl.hidden = false;
      renderOrderList();
    } catch (error) {
      loading.hidden = true;
      errorEl.hidden = false;
      errorEl.textContent = error.message;
    }
  }

  async function submitReview(orderId, cookieId, button) {
    const rating = Number(document.getElementById(`rating-${orderId}-${cookieId}`).value);
    const comment = document.getElementById(`comment-${orderId}-${cookieId}`).value;
    const customerId = window.HomemadeCookieApi.getCustomerId();

    button.disabled = true;
    button.textContent = 'Submitting...';

    try {
      const result = await window.HomemadeCookieApi.createReview({
        orderId,
        customerId,
        cookieId,
        rating,
        comment
      });

      resultBox.hidden = false;
      resultBox.className = 'result success';
      resultBox.textContent = result.message || 'Review submitted successfully!';

      button.textContent = '✅ Review submitted';
      button.disabled = true;
    } catch (error) {
      button.disabled = false;
      button.textContent = 'Submit review';
      resultBox.hidden = false;
      resultBox.className = 'result error';
      resultBox.textContent = error.message;
    }
  }

  tabs.forEach((tab) => {
    tab.addEventListener('click', () => {
      tabs.forEach(t => t.classList.remove('active'));
      tab.classList.add('active');
      selectedFilter = tab.dataset.filter;
      renderOrderList();
    });
  });

  window.HomemadeCookieAuth.requireCustomer().then((user) => {
    if (user) {
      // Store user email for cancellation
      if (user.email) {
        currentEmail = user.email;
        console.log('User email stored for cancellation:', currentEmail);
      }
      loadCustomerOrders();
    }
  });
})();