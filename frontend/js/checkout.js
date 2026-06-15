(function () {
  const cartSection = document.getElementById('checkout-cart');
  const form = document.getElementById('checkout-form');
  const resultBox = document.getElementById('checkout-result');
  const trackHint = document.getElementById('checkout-track');
  const trackLink = document.getElementById('track-order-link');
  const submitBtn = document.getElementById('checkout-submit');

  const cancelBtn = document.getElementById('checkout-cancel');
  const cancelPanel = document.getElementById('checkout-cancel-panel');
  const leaveBtn = document.getElementById('cancel-leave');
  const saveBtn = document.getElementById('cancel-save');
  const continueBtn = document.getElementById('cancel-continue');
  const pendingOrderId = new URLSearchParams(window.location.search).get('pendingOrderId');

  function formatMoney(value) {
    return `RM ${Number(value).toFixed(2)}`;
  }

  function buildCheckoutPayload() {
    return {
      customerId: window.HomemadeCookieApi.getCustomerId(),
      paymentMethod: document.getElementById('paymentMethod').value,
      cardDetails: document.getElementById('cardDetails').value.trim(),
      customerEmail: document.getElementById('customerEmail').value.trim() || null
    };
  }

  async function loadCartPreview() {
    cartSection.innerHTML = '<p>Loading items...</p>';

    try {
      if (pendingOrderId) {
        const order = await window.HomemadeCookieApi.getOrderStatus(pendingOrderId);
        const items = order.items || [];

        if (!items.length) {
          cartSection.innerHTML = '<p>No items found in this pending order.</p>';
          return;
        }

        cartSection.innerHTML = `
          <ul class="checkout-items">
            ${items.map(i => `
              <li>
                ${i.cookieName} × ${i.quantity} — ${formatMoney(i.priceAtPurchase * i.quantity)}
              </li>
            `).join('')}
          </ul>
          <p><strong>Total: ${formatMoney(order.totalAmount)}</strong></p>
        `;

        return;
      }

      const cart = await window.HomemadeCookieApi.getCart();
      const items = cart.items || [];

      if (!items.length) {
        cartSection.innerHTML = '<p>Your cart is empty.</p>';
        submitBtn.disabled = true;
        return;
      }

      cartSection.innerHTML = `
        <ul class="checkout-items">
          ${items.map(item => `
            <li>
              ${item.cookieName} × ${item.quantity} — ${formatMoney(item.lineTotal ?? item.unitPrice * item.quantity)}
            </li>
          `).join('')}
        </ul>
        <p><strong>Total: ${formatMoney(cart.total)}</strong></p>
      `;

      submitBtn.disabled = false;
    } catch (error) {
      cartSection.innerHTML = `<p class="error">${error.message}</p>`;
    }
  }

  form.addEventListener('submit', async (event) => {
    event.preventDefault();
    resultBox.hidden = false;
    resultBox.className = 'result';
    resultBox.textContent = 'Processing checkout via Facade…';
    trackHint.hidden = true;
    submitBtn.disabled = true;

    const payload = buildCheckoutPayload();

    if (!payload.customerId) {
      resultBox.className = 'result error';
      resultBox.textContent = 'Please log in as a customer before checkout.';
      submitBtn.disabled = false;
      return;
    }

    try {
      const result = pendingOrderId
        ? await window.HomemadeCookieApi.payPendingOrder(pendingOrderId, payload)
        : await window.HomemadeCookieApi.checkout(payload);
      const isSuccess = result.success === true;
      const isPending = result.outcome === 'PaymentPending';

      resultBox.className = `result ${isSuccess ? 'success' : isPending ? 'warning' : 'error'}`;
      resultBox.textContent = result.message || 'Checkout complete.';

      if (result.orderId) {
        trackHint.hidden = false;
        trackLink.href = `/track-order.html?orderId=${result.orderId}`;
        trackLink.textContent = `Track order #${result.orderId}`;
        sessionStorage.setItem('lastOrderId', String(result.orderId));
      }

      if (isSuccess) await loadCartPreview();
    } catch (error) {
      resultBox.className = 'result error';
      resultBox.textContent = error.message;
    } finally {
      submitBtn.disabled = false;
    }
  });

  document.addEventListener('DOMContentLoaded', () => {
    const cancelBtn = document.getElementById('checkout-cancel');
    const cancelOverlay = document.getElementById('checkout-cancel-overlay');
    const leaveBtn = document.getElementById('cancel-leave');
    const continueBtn = document.getElementById('cancel-continue');
    const saveBtn = document.getElementById('cancel-save');

    saveBtn.addEventListener('click', async () => {
      const payload = buildCheckoutPayload();

      try {
        saveBtn.disabled = true;
        saveBtn.textContent = 'Saving...';

        const result = await window.HomemadeCookieApi.savePendingOrder(payload);

        alert(result.message || 'Order saved as pending.');

        window.location.href = '/track-order.html?status=Pending';
      } catch (error) {
        console.error(error);
        alert(error.message || 'Failed to save pending order.');
      } finally {
        saveBtn.disabled = false;
        saveBtn.textContent = 'Save as order';
      }
    });

    if (!cancelBtn || !cancelOverlay || !leaveBtn || !continueBtn || !saveBtn) {
      console.error('Cancel checkout elements not found');
      return;
    }

    cancelBtn.addEventListener('click', () => {
      cancelOverlay.hidden = false;

      if (pendingOrderId) {
        saveBtn.hidden = true;
      } else {
        saveBtn.hidden = false;
      }
    });

    leaveBtn.addEventListener('click', () => {
      if (pendingOrderId) {
        window.location.href = '/track-order.html';
      } else {
        window.location.href = '/cart.html';
      }
    });

    continueBtn.addEventListener('click', () => {
      cancelOverlay.hidden = true;
    });
  });

  window.HomemadeCookieAuth.requireCustomer().then((user) => {
    if (user) {
      const emailField = document.getElementById('customerEmail');
      if (emailField && !emailField.value) emailField.value = user.email || '';
      loadCartPreview();
    }
  });
})();
