(function () {
  const loading = document.getElementById('wishlist-loading');
  const empty = document.getElementById('wishlist-empty');
  const list = document.getElementById('wishlist-items');
  const moveBtn = document.getElementById('move-to-cart-btn');
  const message = document.getElementById('wishlist-message');

  function formatMoney(v) {
    return `RM ${Number(v).toFixed(2)}`;
  }

  function showMsg(text, type) {
    message.hidden = false;
    message.className = `result ${type}`;
    message.textContent = text;
  }

  async function render() {
    loading.hidden = false;
    empty.hidden = true;
    list.hidden = true;
    moveBtn.hidden = true;

    try {
      const items = await window.HomemadeCookieApi.getWishlist();
      loading.hidden = true;

      if (!items.length) {
        empty.hidden = false;
        return;
      }

      list.hidden = false;
      moveBtn.hidden = false;
      list.innerHTML = items.map((item) => `
        <article class="wishlist-card">
          <div>
            <strong>${item.cookieName}</strong>
            <p>${formatMoney(item.price)} · Stock: ${item.stock}</p>
            <p class="hint">Added ${new Date(item.addedAt).toLocaleDateString()}</p>
          </div>
          <button type="button" class="btn-secondary btn-remove-wish" data-id="${item.cookieId}">Remove</button>
        </article>
      `).join('');

      list.querySelectorAll('.btn-remove-wish').forEach((btn) => {
        btn.addEventListener('click', async () => {
          await window.HomemadeCookieApi.removeFromWishlist(Number(btn.dataset.id));
          await render();
        });
      });
    } catch (err) {
      loading.hidden = true;
      showMsg(err.message, 'error');
    }
  }

  moveBtn?.addEventListener('click', async () => {
    try {
      const result = await window.HomemadeCookieApi.moveWishlistToCart();
      showMsg(result.message, 'success');
      await render();
    } catch (err) {
      showMsg(err.message, 'error');
    }
  });

  window.HomemadeCookieAuth.requireCustomer().then((user) => {
    if (user) render();
  });
})();
