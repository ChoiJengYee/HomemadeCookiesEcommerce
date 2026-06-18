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
    const image = item.imageUrl || '/images/default-cookie.jpg';
    const original = Number(item.originalPrice ?? item.unitPrice);
    const current = Number(item.unitPrice);
    const originalLineTotal = original * item.quantity;
    const currentLineTotal = current * item.quantity;
    const hasDiscount = currentLineTotal < originalLineTotal;

    const isBox = item.quantity >= 5 && current <= original;
    const hasGiftBox = Math.abs(current - (original + 5)) < 0.01;

    return `
      <article class="cart-row" data-cookie-id="${item.cookieId}">
        <label class="cart-select">
          <input type="checkbox" class="cart-check" checked>
        </label>

        <img class="cart-item-img" src="${image}" alt="${name}">

        <div class="cart-row-info">
          <h3>${name}</h3>

          <div class="cart-tags">
            <span>${isBox ? 'Box package' : 'À la carte'}</span>
            ${hasGiftBox ? '<span>Gift box +RM5</span>' : ''}
          </div>

          <p class="cart-row-price">
            ${hasDiscount
              ? `<span class="old-price">${formatPrice(original)}</span>
                <strong class="discount-price">${formatPrice(current)}</strong>`
              : `<strong>${formatPrice(current)}</strong>`
            }
            <span> each</span>
          </p>
        </div>

        <div class="cart-row-actions">
          <div class="qty-control" aria-label="Quantity for ${name}">
            <button type="button" class="qty-btn" data-action="decrease">−</button>
            <span class="qty-value">${item.quantity}</span>
            <button type="button" class="qty-btn" data-action="increase">+</button>
          </div>

          <p class="line-total" data-line-total="${currentLineTotal}">
            ${hasDiscount
              ? `<span class="line-original-price">${formatPrice(originalLineTotal)}</span>
                <strong class="line-discount-price">${formatPrice(currentLineTotal)}</strong>`
              : `<strong>${formatPrice(currentLineTotal)}</strong>`
            }
          </p>
          <button type="button" class="btn-link danger" data-action="remove">Remove</button>
        </div>
      </article>`;
  }).join('');

  cartTotalEl.textContent = formatPrice(data.total);

  updateSelectedTotal();
}

function updateSelectedTotal() {
  const rows = [...document.querySelectorAll('.cart-row')];
  let total = 0;

  rows.forEach(row => {
    const checked = row.querySelector('.cart-check')?.checked;
    if (!checked) return;

    const lineTotal = Number(row.querySelector('.line-total')?.dataset.lineTotal || 0);
    total += lineTotal;
  });

  cartTotalEl.textContent = formatPrice(total);
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

document.getElementById('checkout-selected')?.addEventListener('click', (event) => {
  const selectedIds = [...document.querySelectorAll('.cart-row')]
    .filter(row => row.querySelector('.cart-check')?.checked)
    .map(row => Number(row.dataset.cookieId));

  if (selectedIds.length === 0) {
    event.preventDefault();
    showMessage('Please select at least one product to checkout.', true);
    return;
  }

  sessionStorage.setItem('selectedCartCookieIds', JSON.stringify(selectedIds));
});

cartItemsEl.addEventListener('change', (event) => {
  if (event.target.classList.contains('cart-check')) {
    updateSelectedTotal();
  }
});

window.HomemadeCookieAuth.requireCustomer().then((user) => {
  if (user) loadCart();
});