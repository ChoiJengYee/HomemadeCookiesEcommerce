-- Add the missing image_url column to the existing cookies table.
-- Run this against your PostgreSQL database before restarting the app.

ALTER TABLE cookies
ADD COLUMN IF NOT EXISTS image_url VARCHAR(500);

-- Optional: assign default image URLs for existing cookie rows.
UPDATE cookies
SET image_url = CASE name
    WHEN 'Chocolate Chip' THEN '/images/chocolate-chip.jfif'
    WHEN 'Dark Chocolate' THEN '/images/dark-chocolate.jfif'
    WHEN 'Strawberry' THEN '/images/strawberry.jfif'
    WHEN 'Orange Zest' THEN '/images/orange-zest.jfif'
    WHEN 'Oatmeal Raisin' THEN '/images/oatmeal-raisin.jpg'
    WHEN 'Peanut Butter' THEN '/images/peanut-butter.jfif'
    ELSE '/images/cookie-default.svg'
END
WHERE image_url IS NULL;
