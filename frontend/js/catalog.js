const { DEMO_CUSTOMER_ID: catalogCustomerId, getProducts, getCart, addToCart } = window.HomemadeCookieApi;

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

function renderProducts(products) {
  if (!products.length) {
    productGridEl.innerHTML = '<p class="empty-state">No cookies in the catalog yet.</p>';
    return;
  }

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
        <button
          type="button"
          class="btn-add"
          data-cookie-id="${p.cookieId}"
          ${outOfStock ? 'disabled' : ''}
        >
          ${outOfStock ? 'Unavailable' : 'Add to cart'}
        </button>
      </article>`;
  }).join('');
}

async function refreshCartBadge() {
  try {
    const cart = await getCart(DEMO_CUSTOMER_ID);
    updateCartBadge(cart.itemCount ?? 0);
  } catch {
    updateCartBadge(0);
  }
}

async function loadCatalog() {
  // #region agent log
  fetch('http://127.0.0.1:7671/ingest/af3b3b42-a4e5-4a88-afab-eb9b34c91827',{method:'POST',headers:{'Content-Type':'application/json','X-Debug-Session-Id':'138024'},body:JSON.stringify({sessionId:'138024',location:'catalog.js:loadCatalog',message:'loadCatalog start',data:{origin:window.location.origin,protocol:window.location.protocol},timestamp:Date.now(),hypothesisId:'A'})}).catch(()=>{});
  // #endregion

  catalogLoadingEl.hidden = false;
  catalogErrorEl.hidden = true;
  productGridEl.innerHTML = '';

  try {
    const products = await getProducts();
    catalogLoadingEl.hidden = true;
    // #region agent log
    fetch('http://127.0.0.1:7671/ingest/af3b3b42-a4e5-4a88-afab-eb9b34c91827',{method:'POST',headers:{'Content-Type':'application/json','X-Debug-Session-Id':'138024'},body:JSON.stringify({sessionId:'138024',location:'catalog.js:loadCatalog',message:'loadCatalog success',data:{productCount:products.length},timestamp:Date.now(),hypothesisId:'C'})}).catch(()=>{});
    // #endregion
    renderProducts(products);
    await refreshCartBadge();
  } catch (err) {
    catalogLoadingEl.hidden = true;
    catalogErrorEl.hidden = false;
    catalogErrorEl.textContent = err.message;
    // #region agent log
    fetch('http://127.0.0.1:7671/ingest/af3b3b42-a4e5-4a88-afab-eb9b34c91827',{method:'POST',headers:{'Content-Type':'application/json','X-Debug-Session-Id':'138024'},body:JSON.stringify({sessionId:'138024',location:'catalog.js:loadCatalog',message:'loadCatalog error',data:{error:err.message},timestamp:Date.now(),hypothesisId:'A'})}).catch(()=>{});
    // #endregion
  }
}

productGridEl.addEventListener('click', async (event) => {
  const button = event.target.closest('.btn-add');
  if (!button || button.disabled) return;

  const cookieId = Number(button.dataset.cookieId);
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
});

loadCatalog();
