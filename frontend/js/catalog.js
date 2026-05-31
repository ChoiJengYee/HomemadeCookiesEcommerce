const { getProducts, getCart, addToCart, addToWishlist } = window.HomemadeCookieApi;

const productGridEl = document.getElementById('product-grid');
const catalogLoadingEl = document.getElementById('catalog-loading');
const catalogErrorEl = document.getElementById('catalog-error');
const catalogMessageEl = document.getElementById('catalog-message');
const cartCountEl = document.getElementById('cart-count');

function formatPrice(amount) {
  return `RM ${Number(amount).toFixed(2)}`;
}

function escapeHtml(text) {
  const div = document.createElement('div');
  div.textContent = text ?? '';
  return div.innerHTML;
}

function updateCartBadge(itemCount) {
  if (!cartCountEl) return;
  cartCountEl.textContent = itemCount > 0 ? String(itemCount) : '';
  cartCountEl.hidden = itemCount <= 0;
}

function showToast(text, isError = false) {
  if (!catalogMessageEl) return;
  catalogMessageEl.textContent = text;
  catalogMessageEl.className = `toast ${isError ? 'error' : 'success'}`;
  catalogMessageEl.hidden = false;
  window.clearTimeout(showToast._timer);
  showToast._timer = window.setTimeout(() => {
    catalogMessageEl.hidden = true;
  }, 3000);
}

function isCustomerLoggedIn() {
  return window.HomemadeCookieAuth?.getUser?.()?.role === 'Customer';
}

function renderProducts(products) {
  if (!products.length) {
    productGridEl.innerHTML = '<p class="empty-state">No cookies in the catalog yet.</p>';
    return;
  }

  const showWishlist = isCustomerLoggedIn();

  productGridEl.innerHTML = products.map((p) => {
    const outOfStock = p.stock <= 0;
    const description = escapeHtml(p.description || 'Freshly baked homemade cookies.');
    const category = escapeHtml(p.categoryName || '');
    return `
      <article class="product-card">
        <span class="product-card-badge">${category}</span>
        <div class="product-card-icon" aria-hidden="true">🍪</div>
        <h3>${escapeHtml(p.name)}</h3>
        <p class="product-desc">${description}</p>
        <p class="product-price">${formatPrice(p.price)}</p>
        <p class="product-stock ${outOfStock ? 'out' : ''}">
          ${outOfStock ? 'Out of stock' : `${p.stock} in stock`}
        </p>
        <div class="product-actions">
          <button
            type="button"
            class="btn-add"
            data-cookie-id="${p.cookieId}"
            data-action="cart"
            ${outOfStock ? 'disabled' : ''}
          >
            ${outOfStock ? 'Unavailable' : 'Add to cart'}
          </button>
          ${showWishlist ? `
            <button type="button" class="btn-secondary btn-wish" data-cookie-id="${p.cookieId}" data-action="wish">
              ♥ Wishlist
            </button>
          ` : ''}
        </div>
      </article>`;
  }).join('');
}

async function refreshCartBadge() {
  if (!isCustomerLoggedIn()) {
    updateCartBadge(0);
    return;
  }
  try {
    const cart = await getCart();
    updateCartBadge(cart.itemCount ?? 0);
  } catch {
    updateCartBadge(0);
  }
}

async function loadCatalog() {
  catalogLoadingEl.hidden = false;
  catalogErrorEl.hidden = true;
  productGridEl.innerHTML = '';

  try {
    const products = await getProducts();
    catalogLoadingEl.hidden = true;
    renderProducts(products);
    await refreshCartBadge();
  } catch (err) {
    catalogLoadingEl.hidden = true;
    catalogErrorEl.hidden = false;
    catalogErrorEl.textContent = err.message;
  }
}

function redirectLogin() {
  const next = encodeURIComponent(window.location.pathname);
  window.location.href = `/login.html?next=${next}`;
}

async function ensureLoggedIn() {
  try {
    const user = await window.HomemadeCookieAuth.refreshFromServer();
    if (!user) {
      redirectLogin();
      return false;
    }
    return true;
  } catch {
    redirectLogin();
    return false;
  }
}

productGridEl.addEventListener('click', async (event) => {
  const button = event.target.closest('button');
  if (!button || button.disabled) return;

  const cookieId = Number(button.dataset.cookieId);
  const action = button.dataset.action;

  if (action === 'wish') {
    if (!isCustomerLoggedIn()) {
      redirectLogin();
      return;
    }
    button.disabled = true;
    try {
      await addToWishlist(cookieId);
      showToast('Added to wishlist!');
    } catch (err) {
      showToast(err.message, true);
    } finally {
      button.disabled = false;
    }
    return;
  }

  if (action === 'cart') {
    if (!isCustomerLoggedIn()) {
      redirectLogin();
      return;
    }
    button.disabled = true;
    try {
      await addToCart(cookieId, 1);
      showToast('Added to cart!');
      await refreshCartBadge();
    } catch (err) {
      showToast(err.message, true);
    } finally {
      button.disabled = false;
    }
  }
});

(async function () {
  if (!(await ensureLoggedIn())) return;
  await loadCatalog();
})();
