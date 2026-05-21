(function () {
  const cartSection = document.getElementById('checkout-cart');
  const form = document.getElementById('checkout-form');
  const resultBox = document.getElementById('checkout-result');
  const trackHint = document.getElementById('checkout-track');
  const trackLink = document.getElementById('track-order-link');
  const submitBtn = document.getElementById('checkout-submit');

  function formatMoney(value) {
    return `RM ${Number(value).toFixed(2)}`;
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

    const payload = {
      customerId: window.HomemadeCookieApi.DEMO_CUSTOMER_ID,
      paymentMethod: document.getElementById('paymentMethod').value,
      cardDetails: document.getElementById('cardDetails').value.trim(),
      customerEmail: document.getElementById('customerEmail').value.trim() || null
    };

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

  loadCartPreview();
})();
