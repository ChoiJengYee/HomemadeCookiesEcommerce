const cartItemsEl = document.getElementById('cart-items');
const cartEmptyEl = document.getElementById('cart-empty');
const cartSummaryEl = document.getElementById('cart-summary');
const cartTotalEl = document.getElementById('cart-total');
const cartCountEl = document.getElementById('cart-count');

function formatPrice(amount) {
  return `RM ${Number(amount).toFixed(2)}`;
}

function escapeHtml(text) {
  const div = document.createElement('div');
  div.textContent = text;
  return div.innerHTML;
}

async function loadCart() {

    let items = [];
    let total = 0;

    // READ ONLY from localStorage
    for (let key in localStorage) {
        if (key.startsWith("cart_")) {
            try {
                const item = JSON.parse(localStorage.getItem(key));
                items.push(item);
                total += item.totalPrice;
            } catch (e) {}
        }
    }

    if (items.length === 0) {
        cartItemsEl.hidden = true;
        cartEmptyEl.hidden = false;
        cartSummaryEl.hidden = true;
        return;
    }

    cartEmptyEl.hidden = true;
    cartItemsEl.hidden = false;
    cartSummaryEl.hidden = false;

    cartItemsEl.innerHTML = items.map(item => {

        let note = "";

        if (item.packageType === "box5" && item.giftBox === "with") {
            note = "🎁 Box of 5 + Gift Box";
        } else if (item.packageType === "box5") {
            note = "📦 Box of 5";
        } else if (item.giftBox === "with") {
            note = "🎁 Gift Box";
        }

        return `
            <article class="cart-row">
                <div>
                    <h3>${item.name}</h3>
                    <small>${note}</small>
                </div>

                <div>
                    <p>RM ${item.totalPrice.toFixed(2)}</p>
                    <p>Qty: ${item.quantity}</p>
                </div>
            </article>
        `;
    }).join('');

    cartTotalEl.textContent = `RM ${total.toFixed(2)}`;
}

loadCart();