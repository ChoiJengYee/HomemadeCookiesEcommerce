CREATE TABLE IF NOT EXISTS reviews (
    review_id SERIAL PRIMARY KEY,
    order_id INT NOT NULL REFERENCES orders(order_id) ON DELETE CASCADE,
    cookie_id INT NOT NULL REFERENCES cookies(cookie_id) ON DELETE CASCADE,
    customer_id INT NOT NULL REFERENCES users(user_id) ON DELETE CASCADE,
    rating INT NOT NULL CHECK (rating >= 1 AND rating <= 5),
    comment TEXT NOT NULL,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    UNIQUE(order_id, cookie_id, customer_id)
);

CREATE INDEX idx_reviews_cookie_id ON reviews(cookie_id);
CREATE INDEX idx_reviews_customer_id ON reviews(customer_id);
CREATE INDEX idx_reviews_order_id ON reviews(order_id);
CREATE INDEX idx_reviews_created_at ON reviews(created_at DESC);