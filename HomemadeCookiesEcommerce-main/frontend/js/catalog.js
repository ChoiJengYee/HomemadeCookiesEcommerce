const {
  getProducts,
  getCart,
  addToCart,
  addToWishlist,
  updateCookie,
  deleteCookie,
  getAdminCategories,
  getCategories
} = window.HomemadeCookieApi;

const productGridEl = document.getElementById('product-grid');
const catalogLoadingEl = document.getElementById('catalog-loading');
const catalogErrorEl = document.getElementById('catalog-error');
const catalogMessageEl = document.getElementById('catalog-message');
const cartCountEl = document.getElementById('cart-count');

let currentProducts = [];
let currentCategories = [];

function formatPrice(amount) {
  return `RM ${Number(amount).toFixed(2)}`;
}

function escapeHtml(text) {
  const div = document.createElement('div');
  div.textContent = text ?? '';
  return div.innerHTML;
}

function getCategoryName(categoryId) {
  if (Number(categoryId) === 1) return 'Best Seller';
  if (Number(categoryId) === 2) return 'Recommended';
  return '';
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

function isAdminLoggedIn() {
  return window.HomemadeCookieAuth?.getUser?.()?.role === 'Admin';
}

function openEditCookieModal(cookie) {
  document.getElementById('editCookieId').value = cookie.cookieId;
  document.getElementById('editCookieName').value = cookie.name;
  document.getElementById('editCookieDescription').value = cookie.description || '';
  document.getElementById('editCookiePrice').value = cookie.price;
  document.getElementById('editCookieStock').value = cookie.stock;
  document.getElementById('editCookieCategoryId').value = cookie.categoryId;
  document.getElementById('edit-cookie-modal').hidden = false;
}

function closeEditCookieModal() {
  document.getElementById('edit-cookie-modal').hidden = true;
}

function renderProducts(products) {
  currentProducts = products;
  const showAdminActions = isAdminLoggedIn();

  if (!products.length) {
    productGridEl.innerHTML = '<p class="empty-state">No cookies in the catalog yet.</p>';
    return;
  }

  const showActions = isCustomerLoggedIn();

  productGridEl.innerHTML = products.map((p) => {
    const outOfStock = p.stock <= 0;
    const description = escapeHtml(p.description || 'Freshly baked homemade cookies.');
    const category = escapeHtml(getCategoryName(p.categoryId));
    const imageUrl = escapeHtml(p.imageUrl ?? '/images/cookie-default.svg');
    return `
      <article class="product-card">
        <span class="product-card-badge">${category}</span>
        <img class="product-card-image" src="${imageUrl}" alt="${escapeHtml(p.name)} image">
        <h3>${escapeHtml(p.name)}</h3>
        <p class="product-desc">${description}</p>
        <p class="product-price">${formatPrice(p.price)}</p>
        <p class="product-stock ${outOfStock ? 'out' : ''}">
          ${outOfStock ? 'Out of stock' : `${p.stock} in stock`}
        </p>
        <div class="product-actions">
          ${showAdminActions ? `
            <button type="button" class="btn-add btn-edit-cookie" data-cookie-id="${p.cookieId}" data-action="edit-cookie">
              ✏️ Edit Cookie
            </button>
            <button type="button" class="btn-delete" data-cookie-id="${p.cookieId}" data-action="delete-cookie">
            🗑️ Delete
          </button>
          
          ` : showActions ? `
            <button
              type="button"
              class="btn-add"
              data-cookie-id="${p.cookieId}"
              data-action="cart"
              ${outOfStock ? 'disabled' : ''}
            >
              ${outOfStock ? 'Unavailable' : 'Add to cart'}
            </button>
            <button type="button" class="btn-secondary btn-wish" data-cookie-id="${p.cookieId}" data-action="wish">
              ♥ Wishlist
            </button>
          ` : `
            <button type="button" class="btn-secondary btn-login" data-action="login">
              👀 Login to order
            </button>
          `}
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

async function loadEditCategories() {
  const select = document.getElementById('editCookieCategoryId');
  if (!select) return;

  select.innerHTML = '<option value="">Loading categories...</option>';

  try {
    const categories = await getAdminCategories();

    select.innerHTML = '<option value="">Select category</option>';

    categories.forEach(category => {
      const option = document.createElement('option');

      option.value = category.categoryId ?? category.id;
      option.textContent = category.name ?? category.categoryName;

      select.appendChild(option);
    });
  } catch (err) {
    console.error(err);
    select.innerHTML = '<option value="">Failed to load categories</option>';
  }
}

async function openEditCookieModal(cookie) {
  if (!currentCategories.length) {
    await loadEditCategories();
  }

  document.getElementById('editCookieId').value = cookie.cookieId;
  document.getElementById('editCookieName').value = cookie.name;
  document.getElementById('editCookieDescription').value = cookie.description || '';
  document.getElementById('editCookiePrice').value = cookie.price;
  document.getElementById('editCookieStock').value = cookie.stock;
  document.getElementById('editCookieCategoryId').value = cookie.categoryId;
  document.getElementById('editImagePreview').src = cookie.imageUrl || '/images/cookie-default.svg';
  document.getElementById('editCookieImageFile').value = '';
  document.getElementById('editCookieCurrentImageUrl').value = cookie.imageUrl || '';

  document.getElementById('edit-cookie-modal').hidden = false;
}

function closeEditCookieModal() {
  document.getElementById('edit-cookie-modal').hidden = true;
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

productGridEl.addEventListener('click', async (event) => {
  const button = event.target.closest('button');
  if (!button || button.disabled) return;

  const cookieId = Number(button.dataset.cookieId);
  const action = button.dataset.action;

  if (action === 'edit-cookie') {
    const cookie = currentProducts.find(p => p.cookieId === cookieId);
    if (cookie) openEditCookieModal(cookie);
    return;
  }

  if (action === 'wish') {
    if (!isCustomerLoggedIn()) {
      const confirmLogin = window.confirm(
        'Wishlist requires registration. Would you like to login or register now?'
      );
      if (confirmLogin) {
        redirectLogin();
      } else {
        showToast('Continue browsing as guest. Login to save favorites.', true);
      }
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

    if (action === 'delete-cookie') {
      const confirmDelete = confirm('Are you sure you want to delete this cookie?');
      if (!confirmDelete) return;
    
      try {
        await deleteCookie(cookieId);
        showToast('Cookie deleted successfully!');
        await loadCatalog();
      } catch (err) {
        showToast(err.message, true);
      }
      return;
    }
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

  if (action === 'login') {
    redirectLogin();
  }
});

document.getElementById('edit-cookie-close')?.addEventListener('click', closeEditCookieModal);
document.getElementById('edit-cookie-discard')?.addEventListener('click', closeEditCookieModal);

document.getElementById('edit-cookie-form')?.addEventListener('submit', async (event) => {
  event.preventDefault();

  const cookieId = Number(document.getElementById('editCookieId').value);

  const formData = new FormData();

  formData.append('name', document.getElementById('editCookieName').value);
  formData.append('description', document.getElementById('editCookieDescription').value);
  formData.append('price', document.getElementById('editCookiePrice').value);
  formData.append('stock', document.getElementById('editCookieStock').value);
  formData.append('categoryId', document.getElementById('editCookieCategoryId').value);
  formData.append('imageUrl', document.getElementById('editCookieCurrentImageUrl').value);

  const imageFile = document.getElementById('editCookieImageFile')?.files[0];

  if (imageFile) {
    formData.append('image', imageFile);
  }

  await updateCookie(cookieId, formData);

  try {
    await updateCookie(cookieId, formData);
    closeEditCookieModal();
    showToast('Cookie updated successfully!');
    await loadCatalog();
  } catch (err) {
    showToast(err.message, true);
  }
});

document.getElementById('editCookieImageFile')?.addEventListener('change', () => {
  const fileInput = document.getElementById('editCookieImageFile');
  const preview = document.getElementById('editImagePreview');

  const file = fileInput.files[0];
  if (!file) return;

  const reader = new FileReader();

  reader.onload = (e) => {
    preview.src = e.target.result;
  };

  reader.readAsDataURL(file);
});

(async function () {
  await loadCatalog();
})();
