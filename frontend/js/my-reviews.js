// my-reviews.js - Working with auth.js

let allReviews = [];
let currentUser = null;

// ========================================
// TOAST NOTIFICATIONS
// ========================================

function showToast(message, type = 'success') {
    const old = document.querySelector(".toast-message");
    if (old) old.remove();

    const div = document.createElement("div");
    div.className = `toast-message ${type}`;
    div.textContent = message;

    document.body.appendChild(div);
    setTimeout(() => div.remove(), 3000);
}

// ========================================
// UTILITY FUNCTIONS
// ========================================

function getStarDisplay(rating) {
    return '★'.repeat(rating) + '☆'.repeat(5 - rating);
}

function formatDate(dateString) {
    if (!dateString) return '';
    try {
        const date = new Date(dateString);
        return date.toLocaleDateString('en-MY', {
            year: 'numeric',
            month: 'long',
            day: 'numeric'
        });
    } catch {
        return dateString;
    }
}

function escapeHtml(text) {
    if (!text) return '';
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
}

// ========================================
// LOAD REVIEWS
// ========================================

async function loadMyReviews() {
    const container = document.getElementById('reviewsList');
    if (!container) return;
    
    container.innerHTML = `
        <div class="loading-state">
            <div class="loading-spinner"></div>
            <p style="margin-top: 15px;">Loading your reviews...</p>
        </div>
    `;

    try {
        // Get current user from auth
        currentUser = window.HomemadeCookieAuth.getUser();
        console.log('Current user from auth:', currentUser);
        
        // If no user, redirect to login
        if (!currentUser) {
            console.log('No user found, redirecting to login...');
            window.location.href = '/login.html';
            return;
        }
        
        // Get customer ID - auth.js uses userId property
        const customerId = currentUser.userId || currentUser.id;
        
        if (!customerId) {
            console.error('No customer ID found:', currentUser);
            showToast('Please login again.', 'error');
            setTimeout(() => {
                window.location.href = '/login.html';
            }, 1500);
            return;
        }

        console.log('Customer ID:', customerId);
        console.log('User name:', currentUser.name || currentUser.email);

        // Get customer reviews
        const reviews = await window.HomemadeCookieApi.getCustomerReviews(customerId);
        allReviews = reviews || [];
        
        updateStats(allReviews);
        displayReviews(allReviews);
        
    } catch (error) {
        console.error('Error loading reviews:', error);
        
        // Check if it's an authentication error (401)
        if (error.status === 401) {
            window.location.href = '/login.html';
            return;
        }
        
        container.innerHTML = `
            <div class="empty-state">
                <div class="icon">⚠️</div>
                <h3>Unable to load reviews</h3>
                <p class="sub-text">${error.message || 'Please try again later.'}</p>
                <button onclick="loadMyReviews()" style="margin-top: 15px; padding: 10px 30px; background: var(--orange); color: white; border: none; border-radius: 25px; cursor: pointer;">
                    Retry
                </button>
            </div>
        `;
    }
}

// ========================================
// DISPLAY REVIEWS
// ========================================

function displayReviews(reviews) {
    const container = document.getElementById('reviewsList');
    if (!container) return;

    if (!reviews || reviews.length === 0) {
        container.innerHTML = `
            <div class="empty-state">
                <div class="icon">🍪</div>
                <h3>No Reviews Yet</h3>
                <p class="sub-text">You haven't written any reviews yet.</p>
                <p class="sub-text" style="font-size: 0.85rem; margin-top: 5px;">
                    Purchase cookies and share your experience!
                </p>
                <a href="/products.html" class="btn-browse">Browse Cookies</a>
            </div>
        `;
        return;
    }

    // Sort by date (newest first)
    const sortedReviews = [...reviews].sort((a, b) => {
        const dateA = new Date(a.createdAt || 0);
        const dateB = new Date(b.createdAt || 0);
        return dateB - dateA;
    });

    let html = '';
    
    sortedReviews.forEach(review => {
        const rating = review.rating || 0;
        const comment = review.comment || '';
        const productName = review.cookieName || 'Unknown Cookie';
        const date = review.createdAt || new Date().toISOString();
        
        html += `
            <div class="review-card" data-review-id="${review.reviewId}">
                <div class="review-header">
                    <div>
                        <div class="review-product">
                            <a href="/product-options.html?id=${review.cookieId}" class="review-product-link">
                                🍪 ${escapeHtml(productName)}
                            </a>
                        </div>
                        <div class="review-rating">${getStarDisplay(rating)}</div>
                    </div>
                    <div class="review-date">${formatDate(date)}</div>
                </div>
                ${comment ? `<div class="review-comment">${escapeHtml(comment)}</div>` : ''}
                <div class="review-actions">
                    <button class="btn-delete-review" onclick="deleteReview(${review.reviewId})">
                        🗑️ Delete Review
                    </button>
                    <a href="/product-options.html?id=${review.cookieId}" class="btn-view-product">
                        👁️ View Product
                    </a>
                </div>
            </div>
        `;
    });

    container.innerHTML = html;
}

// ========================================
// UPDATE STATS
// ========================================

function updateStats(reviews) {
    const total = reviews.length;
    document.getElementById('totalReviews').textContent = total;
    document.getElementById('reviewCount').textContent = `(${total})`;

    if (total > 0) {
        const avg = reviews.reduce((sum, r) => sum + r.rating, 0) / total;
        document.getElementById('avgRating').textContent = avg.toFixed(1);
        
        // Count unique products
        const uniqueProducts = new Set(reviews.map(r => r.cookieId));
        document.getElementById('totalProducts').textContent = uniqueProducts.size;
    } else {
        document.getElementById('avgRating').textContent = '0.0';
        document.getElementById('totalProducts').textContent = '0';
    }
}

// ========================================
// DELETE REVIEW
// ========================================

async function deleteReview(reviewId) {
    if (!confirm('Are you sure you want to delete this review? This action cannot be undone.')) {
        return;
    }

    const card = document.querySelector(`[data-review-id="${reviewId}"]`);
    const button = card?.querySelector('.btn-delete-review');
    
    if (button) {
        button.disabled = true;
        button.textContent = '⏳ Deleting...';
    }

    try {
        await window.HomemadeCookieApi.deleteReview(reviewId);
        showToast('✅ Your review has been deleted successfully!');
        
        // Remove the card from UI
        if (card) {
            card.style.transition = 'all 0.3s ease';
            card.style.opacity = '0';
            card.style.transform = 'translateX(20px)';
            setTimeout(() => {
                card.remove();
                // Update remaining reviews
                allReviews = allReviews.filter(r => r.reviewId !== reviewId);
                updateStats(allReviews);
                
                // If no reviews left, show empty state
                if (allReviews.length === 0) {
                    displayReviews(allReviews);
                }
            }, 300);
        } else {
            // Reload if card not found
            loadMyReviews();
        }
    } catch (error) {
        console.error('Error deleting review:', error);
        showToast(error.message || '❌ Failed to delete review. Please try again.', 'error');
        
        if (button) {
            button.disabled = false;
            button.textContent = '🗑️ Delete Review';
        }
    }
}

// ========================================
// INITIALIZE
// ========================================

document.addEventListener('DOMContentLoaded', async () => {
    console.log('My Reviews page loaded');
    
    // Get user from auth
    currentUser = window.HomemadeCookieAuth.getUser();
    console.log('User from auth:', currentUser);
    
    // Check if user is logged in (must be customer)
    if (!currentUser) {
        console.log('No user found, redirecting to login...');
        // Redirect to login with return URL
        const returnUrl = encodeURIComponent(window.location.pathname + window.location.search);
        window.location.href = `/login.html?next=${returnUrl}`;
        return;
    }
    
    // Check if user is customer (not admin)
    const role = (currentUser.role || '').toLowerCase();
    if (role !== 'customer') {
        console.log('User is not a customer, redirecting...');
        if (role === 'admin') {
            window.location.href = '/admin/dashboard.html';
        } else {
            window.location.href = '/login.html';
        }
        return;
    }
    
    // Load reviews
    loadMyReviews();
});