(function () {
  const form = document.getElementById('track-form');
  const orderIdInput = document.getElementById('orderId');
  const resultBox = document.getElementById('track-result');
  const detailBox = document.getElementById('order-detail');
  const cancelBtn = document.getElementById('cancel-order-btn');

  const params = new URLSearchParams(window.location.search);
  const presetId = params.get('orderId') || sessionStorage.getItem('lastOrderId');
  if (presetId) orderIdInput.value = presetId;

  function formatMoney(value) {
    return `RM ${Number(value).toFixed(2)}`;
  }

  function renderOrder(data) {
    const items = data.items || [];
    detailBox.hidden = false;
    detailBox.innerHTML = `
      <h3>Order #${data.orderId}</h3>
      <p><strong>Status:</strong> ${data.statusName}</p>
      <p><strong>Placed:</strong> ${new Date(data.orderDate).toLocaleString()}</p>
      <p><strong>Total:</strong> ${formatMoney(data.totalAmount)}</p>
      <ul>${items.map((i) => `<li>${i.cookieName} × ${i.quantity} @ ${formatMoney(i.priceAtPurchase)}</li>`).join('')}</ul>
    `;

    cancelBtn.hidden = data.statusId !== 1;
    cancelBtn.dataset.orderId = data.orderId;
  }

  async function loadStatus(orderId) {
    resultBox.hidden = true;
    detailBox.hidden = true;
    cancelBtn.hidden = true;

    try {
      const data = await window.HomemadeCookieApi.getOrderStatus(orderId);
      renderOrder(data);
    } catch (error) {
      resultBox.hidden = false;
      resultBox.className = 'result error';
      resultBox.textContent = error.message;
    }
  }

  form.addEventListener('submit', async (event) => {
    event.preventDefault();
    await loadStatus(Number(orderIdInput.value));
  });

  cancelBtn.addEventListener('click', async () => {
    const orderId = Number(cancelBtn.dataset.orderId);
    if (!orderId) return;

    cancelBtn.disabled = true;
    try {
      const result = await window.HomemadeCookieApi.cancelOrder(orderId);
      resultBox.hidden = false;
      resultBox.className = 'result success';
      resultBox.textContent = result.message;
      await loadStatus(orderId);
    } catch (error) {
      resultBox.hidden = false;
      resultBox.className = 'result error';
      resultBox.textContent = error.message;
    } finally {
      cancelBtn.disabled = false;
    }
  });

  if (presetId) loadStatus(Number(presetId));
})();
