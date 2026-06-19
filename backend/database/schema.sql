-- Homemade Cookie E-commerce — PostgreSQL schema (ERD-aligned)
-- Run against database: homemade_cookie_db

BEGIN;

DROP TABLE IF EXISTS users CASCADE;
DROP TABLE IF EXISTS customers CASCADE;
DROP TABLE IF EXISTS categories CASCADE;
DROP TABLE IF EXISTS cookies CASCADE;
DROP TABLE IF EXISTS carts CASCADE;
DROP TABLE IF EXISTS cart_items CASCADE;
DROP TABLE IF EXISTS wishlists CASCADE;
DROP TABLE IF EXISTS wishlist_items CASCADE;
DROP TABLE IF EXISTS order_status_lookup CASCADE;
DROP TABLE IF EXISTS orders CASCADE;
DROP TABLE IF EXISTS order_items CASCADE;
DROP TABLE IF EXISTS payments CASCADE;
DROP TABLE IF EXISTS reviews CASCADE;

-- ---------------------------------------------------------------------------
-- Users & customers
-- ---------------------------------------------------------------------------
CREATE TABLE users (
    user_id     SERIAL PRIMARY KEY,
    name        VARCHAR(100) NOT NULL,
    email       VARCHAR(255) NOT NULL UNIQUE,
    password    VARCHAR(255) NOT NULL,
    role        VARCHAR(20) NOT NULL CHECK (role IN ('Customer', 'Admin'))
);

CREATE TABLE customers (
    user_id       INTEGER PRIMARY KEY REFERENCES users (user_id) ON DELETE CASCADE,
    address       TEXT NOT NULL,
    phone_number  VARCHAR(20) NOT NULL
);

-- ---------------------------------------------------------------------------
-- Catalog
-- ---------------------------------------------------------------------------
CREATE TABLE categories (
    category_id  SERIAL PRIMARY KEY,
    name         VARCHAR(100) NOT NULL UNIQUE
);

CREATE TABLE cookies (
    cookie_id    SERIAL PRIMARY KEY,
    name         VARCHAR(100) NOT NULL,
    description  TEXT,
    image_url    VARCHAR(500),
    price        DECIMAL(10, 2) NOT NULL CHECK (price >= 0),
    stock        INTEGER NOT NULL DEFAULT 0 CHECK (stock >= 0),
    category_id  INTEGER NOT NULL REFERENCES categories (category_id)
);

-- ---------------------------------------------------------------------------
-- Cart & wishlist (wishlist deferred in MVP UI; tables for ERD fidelity)
-- ---------------------------------------------------------------------------
CREATE TABLE carts (
    cart_id      SERIAL PRIMARY KEY,
    customer_id  INTEGER NOT NULL UNIQUE REFERENCES customers (user_id) ON DELETE CASCADE
);

CREATE TABLE cart_items (
    cart_item_id  SERIAL PRIMARY KEY,
    cart_id       INTEGER NOT NULL REFERENCES carts (cart_id) ON DELETE CASCADE,
    cookie_id     INTEGER NOT NULL REFERENCES cookies (cookie_id),
    quantity      INTEGER NOT NULL CHECK (quantity > 0),
    unit_price    DECIMAL(10, 2) NOT NULL CHECK (unit_price >= 0),
    UNIQUE (cart_id, cookie_id)
);

CREATE TABLE wishlists (
    wishlist_id   SERIAL PRIMARY KEY,
    customer_id   INTEGER NOT NULL UNIQUE REFERENCES customers (user_id) ON DELETE CASCADE
);

CREATE TABLE wishlist_items (
    wishlist_item_id  SERIAL PRIMARY KEY,
    wishlist_id       INTEGER NOT NULL REFERENCES wishlists (wishlist_id) ON DELETE CASCADE,
    cookie_id         INTEGER NOT NULL REFERENCES cookies (cookie_id),
    added_at          TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UNIQUE (wishlist_id, cookie_id)
);

-- ---------------------------------------------------------------------------
-- Orders & payments
-- ---------------------------------------------------------------------------
CREATE TABLE order_status_lookup (
    status_id    SERIAL PRIMARY KEY,
    status_name  VARCHAR(50) NOT NULL UNIQUE
);

CREATE TABLE orders (
    order_id      SERIAL PRIMARY KEY,
    customer_id   INTEGER NOT NULL REFERENCES customers (user_id),
    order_date    TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    total_amount  DECIMAL(10, 2) NOT NULL CHECK (total_amount >= 0),
    status_id     INTEGER NOT NULL REFERENCES order_status_lookup (status_id)
);

CREATE TABLE order_items (
    order_item_id       SERIAL PRIMARY KEY,
    order_id            INTEGER NOT NULL REFERENCES orders (order_id) ON DELETE CASCADE,
    cookie_id           INTEGER NOT NULL REFERENCES cookies (cookie_id),
    quantity            INTEGER NOT NULL CHECK (quantity > 0),
    price_at_purchase   DECIMAL(10, 2) NOT NULL CHECK (price_at_purchase >= 0)
);

CREATE TABLE payments (
    payment_id  SERIAL PRIMARY KEY,
    order_id    INTEGER NOT NULL UNIQUE REFERENCES orders (order_id) ON DELETE CASCADE,
    method      VARCHAR(50) NOT NULL,
    amount      DECIMAL(10, 2) NOT NULL CHECK (amount >= 0),
    status      VARCHAR(30) NOT NULL
);

-- ---------------------------------------------------------------------------
-- Reviews (deferred in MVP UI; table for ERD fidelity)
-- ---------------------------------------------------------------------------
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

-- ---------------------------------------------------------------------------
-- Indexes
-- ---------------------------------------------------------------------------
CREATE INDEX idx_cookies_category ON cookies (category_id);
CREATE INDEX idx_orders_customer ON orders (customer_id);
CREATE INDEX idx_orders_status ON orders (status_id);
CREATE INDEX idx_order_items_order ON order_items (order_id);
CREATE INDEX idx_cart_items_cart ON cart_items (cart_id);

CREATE INDEX idx_reviews_cookie_id ON reviews(cookie_id);
CREATE INDEX idx_reviews_customer_id ON reviews(customer_id);
CREATE INDEX idx_reviews_order_id ON reviews(order_id);
CREATE INDEX idx_reviews_created_at ON reviews(created_at DESC);

COMMIT;
