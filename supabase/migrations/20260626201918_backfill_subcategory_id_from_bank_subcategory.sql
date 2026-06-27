-- Backfill subcategory_id for transactions where bank_subcategory matches an existing subcategory name
-- This fixes transactions imported before subcategory auto-matching was fully implemented.

UPDATE transactions t
SET subcategory_id = s.id
FROM categories c
JOIN subcategories s ON s.category_id = c.id
WHERE t.category_id = c.id
  AND t.subcategory_id IS NULL
  AND t.bank_subcategory IS NOT NULL
  AND LOWER(TRIM(t.bank_subcategory)) = LOWER(TRIM(s.name));
