// product-options.js - Complete with Reviews (CRUD)

const urlParams = new URLSearchParams(window.location.search);
const cookieId = urlParams.get("id");

let productData = null;
let selectedPackage = "single";
let selectedGift = "none";
let currentUserId = null;
let currentUser = null;
let allReviews = [];

function showToast(msg, isError = false) {
    const old = document.querySelector(".toast-message");
    if (old) old.remove();

    const div = document.createElement("div");
    div.className = "toast-message";
    div.textContent = msg;
    if (isError) div.style.background = "#dc3545";

    document.body.appendChild(div);
    setTimeout(() => div.remove(), 3000);
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
    const priceElement = document.getElementById("totalPrice");
    if (priceElement) {
        priceElement.textContent = `RM ${total.toFixed(2)}`;
    }
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

// ========================
// REVIEWS - FETCH FROM DATABASE
// ========================

async function loadReviews(productId) {
    const container = document.getElementById('reviewsContainer');
    if (!container) {
        console.error('Reviews container not found');
        return;
    }
    
    try {
        container.innerHTML = `
            <div style="text-align: center; padding: 15px; color: var(--text-muted);">
                <p style="font-size: 0.9rem;">Loading reviews...</p>
            </div>
        `;
        
        let reviews = [];
        
        // Fetch from API
        try {
            reviews = await window.HomemadeCookieApi.getProductReviews(productId);
            console.log(`✅ Found ${reviews.length} real reviews from database`);
        } catch (error) {
            console.log('❌ Error fetching product reviews:', error);
        }
        
        // Store reviews
        allReviews = reviews;
        
        // Display reviews
        if (reviews && reviews.length > 0) {
            displayReviews(reviews, container);
        } else {
            container.innerHTML = `
                <div style="text-align: center; padding: 20px; color: var(--text-muted);">
                    <p style="margin-bottom: 5px; font-size: 1rem;">🍪 No reviews yet</p>
                    <p style="font-size: 0.85rem;">Be the first to review this cookie!</p>
                    ${currentUserId ? `
                        <button onclick="showReviewForm()" style="margin-top: 10px; padding: 8px 20px; background: var(--orange); color: white; border: none; border-radius: 20px; cursor: pointer;">
                            Write a Review
                        </button>
                    ` : `
                        <p style="font-size: 0.75rem; margin-top: 5px;">
                            <a href="/login.html" style="color: var(--orange);">Login</a> to write a review
                        </p>
                    `}
                </div>
            `;
        }
        
    } catch (error) {
        console.error('❌ Error loading reviews:', error);
        if (container) {
            container.innerHTML = `
                <div style="text-align: center; padding: 15px; color: var(--text-muted);">
                    <p>Unable to load reviews at this time.</p>
                </div>
            `;
        }
    }
}

function displayReviews(reviews, container) {
    const validReviews = reviews.filter(r => 
        (r.comment) || 
        (r.rating && r.rating > 0)
    );
    
    if (!validReviews || validReviews.length === 0) {
        container.innerHTML = `
            <div style="text-align: center; padding: 20px; color: var(--text-muted);">
                <p style="margin-bottom: 5px;">🍪 No reviews yet</p>
                <p style="font-size: 0.85rem;">Be the first to review this cookie!</p>
                ${currentUserId ? `
                    <button onclick="showReviewForm()" style="margin-top: 10px; padding: 8px 20px; background: var(--orange); color: white; border: none; border-radius: 20px; cursor: pointer;">
                        Write a Review
                    </button>
                ` : `
                    <p style="font-size: 0.75rem; margin-top: 5px;">
                        <a href="/login.html" style="color: var(--orange);">Login</a> to write a review
                    </p>
                `}
            </div>
        `;
        return;
    }
    
    // Sort by date (newest first)
    const sortedReviews = validReviews.sort((a, b) => {
        const dateA = new Date(a.createdAt || 0);
        const dateB = new Date(b.createdAt || 0);
        return dateB - dateA;
    });
    
    let html = '';
    
    // Rating summary
    const ratings = sortedReviews.filter(r => r.rating).map(r => r.rating);
    if (ratings.length > 0) {
        const avgRating = ratings.reduce((a, b) => a + b, 0) / ratings.length;
        const fullStars = Math.round(avgRating);
        const emptyStars = 5 - fullStars;
        
        html += `
            <div style="display: flex; align-items: center; gap: 12px; margin-bottom: 15px; padding: 12px; background: var(--cream-section); border-radius: 10px; border: 1px solid var(--border);">
                <div style="font-size: 1.8rem; color: #f5c518; line-height: 1;">
                    ${'★'.repeat(fullStars)}${'☆'.repeat(emptyStars)}
                </div>
                <div>
                    <div style="font-weight: 700; font-size: 1.1rem; color: var(--text);">${avgRating.toFixed(1)} / 5.0</div>
                    <div style="font-size: 0.85rem; color: var(--text-muted);">${ratings.length} review${ratings.length > 1 ? 's' : ''}</div>
                </div>
                ${currentUserId ? `
                    <button onclick="showReviewForm()" style="margin-left: auto; padding: 6px 16px; background: var(--orange); color: white; border: none; border-radius: 20px; cursor: pointer; font-size: 0.85rem;">
                        Write Review
                    </button>
                ` : ''}
            </div>
        `;
    }
    
    // Individual reviews
    const displayCount = Math.min(sortedReviews.length, 5);
    const displayReviews = sortedReviews.slice(0, displayCount);
    
    displayReviews.forEach(review => {
        const userName = review.customerName || review.userName || 'Anonymous';
        const rating = review.rating || 0;
        const comment = review.comment || '';
        const date = review.createdAt || new Date().toISOString();
        const isOwnReview = currentUserId && review.customerId === currentUserId;
        
        const fullStars = Math.floor(rating);
        const emptyStars = 5 - fullStars;
        
        const formattedDate = formatDate(date);
        
        html += `
            <div class="review-item" style="border-bottom: 1px solid var(--border); padding: 12px 0;">
                <div style="display: flex; justify-content: space-between; align-items: flex-start; margin-bottom: 5px;">
                    <div>
                        <span style="font-weight: 600; color: var(--brown-soft);">${escapeHtml(userName)}</span>
                        ${isOwnReview ? '<span style="font-size: 0.7rem; background: var(--orange); color: white; padding: 2px 8px; border-radius: 10px; margin-left: 5px;">You</span>' : ''}
                        <span style="color: #f5c518; margin-left: 8px; font-size: 0.9rem;">
                            ${'★'.repeat(fullStars)}${'☆'.repeat(emptyStars)}
                        </span>
                    </div>
                    <div style="display: flex; align-items: center; gap: 8px;">
                        <span style="font-size: 0.75rem; color: var(--text-muted);">
                            ${formattedDate}
                        </span>
                        ${isOwnReview ? `
                            <button onclick="deleteOwnReview(${review.reviewId})" 
                                    style="background: none; border: none; color: #dc3545; cursor: pointer; font-size: 0.8rem; padding: 4px 8px; border-radius: 4px;"
                                    onmouseover="this.style.background='#ffebee'" 
                                    onmouseout="this.style.background='none'"
                                    title="Delete your review">
                                🗑️
                            </button>
                        ` : ''}
                    </div>
                </div>
                ${comment ? `<p style="margin: 0; font-size: 0.9rem; color: var(--text); line-height: 1.5;">${escapeHtml(comment)}</p>` : ''}
            </div>
        `;
    });
    
    if (sortedReviews.length > 5) {
        html += `
            <div style="text-align: center; margin-top: 10px;">
                <button onclick="loadAllReviews()" style="background: none; border: 1px solid var(--border); padding: 8px 20px; border-radius: 20px; cursor: pointer; color: var(--brown-soft);">
                    View All ${sortedReviews.length} Reviews
                </button>
            </div>
        `;
    }
    
    container.innerHTML = html;
}

// ========================
// REVIEW FORM
// ========================

function showReviewForm() {
    if (!currentUserId) {
        showToast('Please login to write a review', true);
        setTimeout(() => {
            window.location.href = '/login.html';
        }, 1500);
        return;
    }
    
    const container = document.getElementById('reviewsContainer');
    if (!container) return;
    
    container.innerHTML = `
        <div style="background: var(--cream-section); border-radius: 12px; padding: 20px; border: 1px solid var(--border);">
            <h4 style="margin-bottom: 15px; color: var(--brown-soft);">Write a Review for ${productData?.name || 'Cookie'}</h4>
            <form id="reviewForm" onsubmit="submitReview(event)">
                <div style="margin-bottom: 15px;">
                    <label style="display: block; margin-bottom: 5px; font-weight: 600;">Rating</label>
                    <div id="starRating" style="font-size: 2rem; color: #d3d3d3; cursor: pointer;">
                        <span onclick="setRating(1)" onmouseover="hoverRating(1)" onmouseout="resetHover()">★</span>
                        <span onclick="setRating(2)" onmouseover="hoverRating(2)" onmouseout="resetHover()">★</span>
                        <span onclick="setRating(3)" onmouseover="hoverRating(3)" onmouseout="resetHover()">★</span>
                        <span onclick="setRating(4)" onmouseover="hoverRating(4)" onmouseout="resetHover()">★</span>
                        <span onclick="setRating(5)" onmouseover="hoverRating(5)" onmouseout="resetHover()">★</span>
                    </div>
                    <input type="hidden" id="ratingInput" value="0">
                </div>
                <div style="margin-bottom: 15px;">
                    <label style="display: block; margin-bottom: 5px; font-weight: 600;">Your Review</label>
                    <textarea id="reviewComment" rows="3" placeholder="Share your experience with this cookie" style="width: 100%; padding: 8px 12px; border: 1px solid var(--border); border-radius: 8px;"></textarea>
                </div>
                <div style="display: flex; gap: 10px;">
                    <button type="submit" style="padding: 10px 24px; background: var(--orange); color: white; border: none; border-radius: 8px; cursor: pointer; font-weight: 600;">
                        Submit Review
                    </button>
                    <button type="button" onclick="loadReviews(cookieId)" style="padding: 10px 24px; background: var(--border); color: var(--text); border: none; border-radius: 8px; cursor: pointer;">
                        Cancel
                    </button>
                </div>
            </form>
        </div>
    `;
}

let selectedRating = 0;

function setRating(rating) {
    selectedRating = rating;
    document.getElementById('ratingInput').value = rating;
    updateStarDisplay(rating);
}

function hoverRating(rating) {
    updateStarDisplay(rating);
}

function resetHover() {
    updateStarDisplay(selectedRating);
}

function updateStarDisplay(rating) {
    const stars = document.querySelectorAll('#starRating span');
    stars.forEach((star, index) => {
        if (index < rating) {
            star.style.color = '#f5c518';
        } else {
            star.style.color = '#d3d3d3';
        }
    });
}

async function submitReview(event) {
    event.preventDefault();
    
    const rating = parseInt(document.getElementById('ratingInput').value);
    const comment = document.getElementById('reviewComment').value.trim();
    
    if (!rating || rating === 0) {
        showToast('Please select a rating', true);
        return;
    }
    
    if (!comment) {
        showToast('Please write a review', true);
        return;
    }
    
    const reviewData = {
        orderId: 0, // This will be validated by the backend
        cookieId: parseInt(cookieId),
        rating: rating,
        comment: comment
    };
    
    try {
        const result = await window.HomemadeCookieApi.submitReview(reviewData);
        showToast(result.message || '✅ Review submitted successfully! Thank you for your feedback!');
        // Reload reviews after submission
        setTimeout(() => {
            loadReviews(cookieId);
        }, 500);
    } catch (error) {
        console.error('Error submitting review:', error);
        showToast(error.message || '❌ Failed to submit review. Please try again.', true);
    }
}

// ========================
// DELETE OWN REVIEW
// ========================

async function deleteOwnReview(reviewId) {
    if (!confirm('Are you sure you want to delete your review? This action cannot be undone.')) {
        return;
    }
    
    try {
        await window.HomemadeCookieApi.deleteReview(reviewId);
        showToast('✅ Your review has been deleted successfully!');
        
        // Reload reviews
        setTimeout(() => {
            loadReviews(cookieId);
        }, 500);
    } catch (error) {
        console.error('Error deleting review:', error);
        showToast(error.message || '❌ Failed to delete review. Please try again.', true);
    }
}

// ========================
// UTILITY FUNCTIONS
// ========================

function escapeHtml(text) {
    if (!text) return '';
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
}

function formatDate(dateString) {
    if (!dateString) return '';
    try {
        const date = new Date(dateString);
        const options = { 
            year: 'numeric', 
            month: 'short', 
            day: 'numeric' 
        };
        return date.toLocaleDateString('en-MY', options);
    } catch {
        return dateString;
    }
}

function loadAllReviews() {
    if (!allReviews || allReviews.length === 0) {
        showToast('No reviews available', true);
        return;
    }
    
    const container = document.getElementById('reviewsContainer');
    if (!container) return;
    
    displayReviews(allReviews, container);
    showToast(`Showing all ${allReviews.length} reviews`);
}

// ========================
// PRODUCT LOADING
// ========================

async function loadProduct() {
    try {
        const products = await window.HomemadeCookieApi.getProducts();
        productData = products.find(p => p.cookieId == cookieId);

        if (!productData) throw new Error("Product not found");

        const singlePrice = document.getElementById("singlePrice");
        const boxPrice = document.getElementById("boxPrice");
        const productImage = document.getElementById("productImage");
        const productTitle = document.getElementById("productTitle");
        const productDesc = document.getElementById("productDesc");
        const loadingState = document.getElementById("loadingState");
        const errorState = document.getElementById("errorState");
        const productContent = document.getElementById("productContent");

        if (singlePrice) singlePrice.textContent = `RM ${productData.price.toFixed(2)}`;
        if (boxPrice) boxPrice.textContent = `RM ${((productData.price * 5) - 5).toFixed(2)}`;
        if (productImage) productImage.src = productData.imageUrl || '/images/default-cookie.jpg';
        if (productTitle) productTitle.textContent = productData.name;
        if (productDesc) productDesc.textContent = productData.description || 'Delicious homemade cookie';

        if (loadingState) loadingState.style.display = "none";
        if (errorState) errorState.style.display = "none";
        if (productContent) productContent.style.display = "block";

        selectPackage("single");
        selectGift("none");
        
        // Load reviews from database
        await loadReviews(productData.cookieId);

    } catch (err) {
        console.error(err);
        const loadingState = document.getElementById("loadingState");
        const errorState = document.getElementById("errorState");
        if (loadingState) loadingState.style.display = "none";
        if (errorState) errorState.style.display = "block";
    }
}

// ========================
// ADD TO CART
// ========================

async function addToCart() {
    const btn = document.getElementById("addToCartBtn");
    if (!btn) return;
    
    btn.disabled = true;
    btn.textContent = "Adding...";

    try {
        const { total, quantity } = calculatePrice();

        await window.HomemadeCookieApi.addToCart(
            productData.cookieId,
            quantity,
            {
                customPrice: total / quantity,
                packageType: selectedPackage,
                giftBox: selectedGift,
                itemDescription: productData.name
            }
        );

        showToast(`✅ Added RM ${total.toFixed(2)} to cart`);

        setTimeout(() => {
            window.location.href = "/cart.html";
        }, 500);

    } catch (err) {
        console.error(err);
        showToast("❌ Failed to add to cart. Please try again.", true);
        btn.disabled = false;
        btn.textContent = "Add to Cart";
    }
}

// ========================
// EVENT LISTENERS
// ========================

document.addEventListener("DOMContentLoaded", async () => {
    try {
        currentUser = await window.HomemadeCookieApi.getMe();
        currentUserId = currentUser?.id || null;
        console.log('User logged in:', currentUserId);
    } catch (error) {
        console.log('User not logged in');
        currentUserId = null;
    }
    
    document.querySelectorAll('[data-option="package"]').forEach(el => {
        el.addEventListener("click", () => selectPackage(el.dataset.value));
    });

    document.querySelectorAll('[data-option="gift"]').forEach(el => {
        el.addEventListener("click", () => selectGift(el.dataset.value));
    });

    const addToCartBtn = document.getElementById("addToCartBtn");
    if (addToCartBtn) {
        addToCartBtn.addEventListener("click", addToCart);
    }

    loadProduct();
});

window.addEventListener("pageshow", () => {
    const btn = document.getElementById("addToCartBtn");
    if (btn) {
        btn.disabled = false;
        btn.textContent = "Add to Cart";
    }
});