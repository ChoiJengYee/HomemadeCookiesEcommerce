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
    try {
      const cart = await window.HomemadeCookieApi.getCart();
      const items = cart.items || [];

      if (items.length === 0) {
        cartSection.innerHTML = '<p class="empty-state">Cart is empty. <a href="/index.html">Add cookies</a> first.</p>';
        submitBtn.disabled = true;
        return;
      }

      const total = items.reduce((sum, i) => sum + i.quantity * i.unitPrice, 0);
      cartSection.innerHTML = `
        <ul class="checkout-line-list">
          ${items.map((i) => `
            <li><span>${i.cookieName}</span> × ${i.quantity} — ${formatMoney(i.quantity * i.unitPrice)}</li>
          `).join('')}
        </ul>
        <p class="checkout-total"><strong>Total: ${formatMoney(total)}</strong></p>
      `;
      submitBtn.disabled = false;
    } catch (error) {
      cartSection.innerHTML = `<p class="status-text error">${error.message}</p>`;
      submitBtn.disabled = true;
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
      const result = await window.HomemadeCookieApi.checkout(payload);
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

    if (!cancelBtn || !cancelPanel || !leaveBtn || !continueBtn) {
      console.error('Cancel checkout elements not found');
      return;
    }

    cancelBtn.addEventListener('click', () => {
      console.log('Cancel checkout clicked');
      cancelOverlay.hidden = false;
    });

    leaveBtn.addEventListener('click', () => {
      window.location.href = '/cart.html';
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
