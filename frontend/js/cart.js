const { getCart, updateCartItem, removeFromCart } = window.HomemadeCookieApi;

const cartItemsEl = document.getElementById('cart-items');
const cartEmptyEl = document.getElementById('cart-empty');
const cartSummaryEl = document.getElementById('cart-summary');
const cartTotalEl = document.getElementById('cart-total');
const cartCountEl = document.getElementById('cart-count');
const cartMessageEl = document.getElementById('cart-message');

function formatPrice(amount) {
  return `RM ${Number(amount).toFixed(2)}`;
}

function escapeHtml(text) {
  const div = document.createElement('div');
  div.textContent = text;
  return div.innerHTML;
}

function showMessage(text, isError = false) {
  if (!cartMessageEl) return;
  cartMessageEl.textContent = text;
  cartMessageEl.className = `result ${isError ? 'error' : 'success'}`;
  cartMessageEl.hidden = false;
  window.clearTimeout(showMessage._timer);
  showMessage._timer = window.setTimeout(() => {
    cartMessageEl.hidden = true;
  }, 3500);
}

function updateCartBadge(itemCount) {
  if (!cartCountEl) return;
  cartCountEl.textContent = itemCount > 0 ? String(itemCount) : '';
  cartCountEl.hidden = itemCount <= 0;
}

function renderCart(data) {
  const items = data.items || [];
  const totalQty = data.itemCount ?? items.reduce((sum, i) => sum + i.quantity, 0);
  updateCartBadge(totalQty);

  if (items.length === 0) {
    cartItemsEl.innerHTML = '';
    cartItemsEl.hidden = true;
    cartEmptyEl.hidden = false;
    cartSummaryEl.hidden = true;
    return;
  }

  cartEmptyEl.hidden = true;
  cartItemsEl.hidden = false;
  cartSummaryEl.hidden = false;

  cartItemsEl.innerHTML = items.map((item) => {
    const name = escapeHtml(item.cookieName);
    return `
      <article class="cart-row" data-cookie-id="${item.cookieId}">
        <div class="cart-row-info">
          <h3>${name}</h3>
          <p class="cart-row-price">${formatPrice(item.unitPrice)} each</p>
        </div>
        <div class="cart-row-actions">
          <div class="qty-control" aria-label="Quantity for ${name}">
            <button type="button" class="qty-btn" data-action="decrease" aria-label="Decrease quantity">−</button>
            <span class="qty-value">${item.quantity}</span>
            <button type="button" class="qty-btn" data-action="increase" aria-label="Increase quantity">+</button>
          </div>
          <p class="line-total">${formatPrice(item.lineTotal)}</p>
          <button type="button" class="btn-link danger" data-action="remove">Remove</button>
        </div>
      </article>`;
  }).join('');

  cartTotalEl.textContent = formatPrice(data.total);
}

async function loadCart() {
  try {
    const data = await getCart();
    renderCart(data);
  } catch (err) {
    showMessage(err.message, true);
  }
}

cartItemsEl.addEventListener('click', async (event) => {
  const button = event.target.closest('button');
  if (!button) return;

  const row = button.closest('.cart-row');
  if (!row) return;

  const cookieId = Number(row.dataset.cookieId);
  const qtyEl = row.querySelector('.qty-value');
  const quantity = Number(qtyEl.textContent);
  const action = button.dataset.action;

  try {
    if (action === 'remove') {
      await removeFromCart(cookieId);
      showMessage('Item removed.');
    } else if (action === 'increase') {
      await updateCartItem(cookieId, quantity + 1);
    } else if (action === 'decrease') {
      if (quantity <= 1) {
        await removeFromCart(cookieId);
        showMessage('Item removed.');
      } else {
        await updateCartItem(cookieId, quantity - 1);
      }
    } else {
      return;
    }

    await loadCart();
  } catch (err) {
    showMessage(err.message, true);
  }
});

window.HomemadeCookieAuth.requireCustomer().then((user) => {
  if (user) loadCart();
});
