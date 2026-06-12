-- Demo seed data for homemade_cookie_db
-- Passwords are plain text for prototype/demo only.

BEGIN;

-- Order statuses (IDs fixed for State pattern hydration)
INSERT INTO order_status_lookup (status_id, status_name) VALUES
    (1, 'Pending'),
    (2, 'Confirmed'),
    (3, 'Baking'),
    (4, 'Ready'),
    (5, 'Completed'),
    (6, 'Cancelled');

SELECT setval(pg_get_serial_sequence('order_status_lookup', 'status_id'), 6);

-- Users: admin (1), customer (2)
INSERT INTO users (user_id, name, email, password, role) VALUES
    (1, 'Bakery Admin', 'admin@homemadecookies.com', 'admin123', 'Admin'),
    (2, 'Demo Customer', 'customer@homemadecookies.com', 'customer123', 'Customer');

SELECT setval(pg_get_serial_sequence('users', 'user_id'), 2);

INSERT INTO customers (user_id, address, phone_number) VALUES
    (2, '123 Cookie Lane, Sweetville', '+60123456789');

-- Categories
INSERT INTO categories (category_id, name) VALUES
    (1, 'Best Seller'),
    (2, 'Recommended');

SELECT setval(pg_get_serial_sequence('categories', 'category_id'), 2);

-- Sample cookies (factory types represented in catalog)
INSERT INTO cookies (cookie_id, name, description, image_url, price, stock, category_id) VALUES
    (1, 'Chocolate Chip', 'Classic buttery cookies with rich chocolate chips.', '/images/chocolate-chip.jfif', 12.00, 50, 1),
    (2, 'Dark Chocolate', 'Intense cocoa cookies for chocolate lovers.', '/images/dark-chocolate.jfif', 14.00, 40, 1),
    (3, 'Strawberry', 'Soft cookies with real strawberry pieces.', '/images/strawberry.jfif', 13.00, 35, 2),
    (4, 'Orange Zest', 'Bright citrus cookies with orange zest.', '/images/orange-zest.jfif', 11.50, 30, 2),
    (5, 'Oatmeal Raisin', 'Hearty oatmeal cookies with plump raisins.', '/images/oatmeal-raisin.jpg', 10.00, 45, 2),
    (6, 'Peanut Butter', 'Creamy peanut butter cookies with a crisp edge.', '/images/peanut-butter.jfif', 12.50, 25, 1);

SELECT setval(pg_get_serial_sequence('cookies', 'cookie_id'), 6);

-- Customer cart (empty; cart_items added during demo checkout)
INSERT INTO carts (cart_id, customer_id) VALUES (1, 2);
SELECT setval(pg_get_serial_sequence('carts', 'cart_id'), 1);

INSERT INTO wishlists (wishlist_id, customer_id) VALUES (1, 2);
SELECT setval(pg_get_serial_sequence('wishlists', 'wishlist_id'), 1);

COMMIT;
