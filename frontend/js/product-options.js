const urlParams = new URLSearchParams(window.location.search);
const cookieId = urlParams.get("id");

let productData = null;
let selectedPackage = "single";
let selectedGift = "none";

function showToast(msg, isError = false) {
    const old = document.querySelector(".toast-message");
    if (old) old.remove();

    const div = document.createElement("div");
    div.className = "toast-message";
    div.textContent = msg;
    if (isError) div.style.background = "#9b2226";

    document.body.appendChild(div);
    setTimeout(() => div.remove(), 2000);
}

function calculatePrice() {
    const price = productData.price;

    let base =
        selectedPackage === "single"
            ? price
            : (price * 5) - 5;

    if (selectedGift === "with") {
        base += 5;
    }

    const quantity = selectedPackage === "single" ? 1 : 5;

    return { total: base, quantity };
}

function updateUIPrice() {
    const { total } = calculatePrice();
    document.getElementById("totalPrice").textContent = `RM ${total.toFixed(2)}`;
}

function selectPackage(value) {
    selectedPackage = value;

    document.querySelectorAll('[data-option="package"]').forEach(el => {
        el.classList.toggle("selected", el.dataset.value === value);
    });

    updateUIPrice();
}

function selectGift(value) {
    selectedGift = value;

    document.querySelectorAll('[data-option="gift"]').forEach(el => {
        el.classList.toggle("selected", el.dataset.value === value);
    });

    updateUIPrice();
}

async function loadProduct() {
    try {
        const products = await window.HomemadeCookieApi.getProducts();
        productData = products.find(p => p.cookieId == cookieId);

        if (!productData) throw new Error("Not found");

        document.getElementById("singlePrice").textContent =
            `RM ${productData.price.toFixed(2)}`;

        document.getElementById("boxPrice").textContent =
            `RM ${((productData.price * 5) - 5).toFixed(2)}`;

        document.getElementById("productImage").src = productData.imageUrl;
        document.getElementById("productTitle").textContent = productData.name;
        document.getElementById("productDesc").textContent = productData.description;

        document.getElementById("loadingState").style.display = "none";
        document.getElementById("productContent").style.display = "block";

        selectPackage("single");
        selectGift("none");

    } catch (err) {
        console.error(err);
        document.getElementById("loadingState").style.display = "none";
        document.getElementById("errorState").style.display = "block";
    }
}

async function addToCart() {
    const btn = document.getElementById("addToCartBtn");

    btn.disabled = true;
    btn.textContent = "Adding...";

    try {
        const { total, quantity } = calculatePrice();

        await window.HomemadeCookieApi.addToCart(
            productData.cookieId,
            quantity,
            {
                customPrice: total,
                packageType: selectedPackage,
                giftBox: selectedGift,
                itemDescription: productData.name
            }
        );

        showToast(`Added RM ${total.toFixed(2)}`);

        setTimeout(() => {
            window.location.href = "/cart.html";
        }, 500);

    } catch (err) {
        console.error(err);
        showToast("Failed to add", true);

        btn.disabled = false;
        btn.textContent = "Add to Cart";
    }
}

document.addEventListener("DOMContentLoaded", () => {

    document.querySelectorAll('[data-option="package"]').forEach(el => {
        el.addEventListener("click", () => selectPackage(el.dataset.value));
    });

    document.querySelectorAll('[data-option="gift"]').forEach(el => {
        el.addEventListener("click", () => selectGift(el.dataset.value));
    });

    document.getElementById("addToCartBtn")
        .addEventListener("click", addToCart);

    loadProduct();
});