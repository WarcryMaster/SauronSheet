-- Feature 3: System Categories Global Scope Refactoring
-- Migrates system default categories from in-memory to persisted with NULL user_id

-- Step 1: Make user_id nullable (non-breaking for existing user categories)
ALTER TABLE public.categories ALTER COLUMN user_id DROP NOT NULL;

-- Step 2: Update UNIQUE constraint to exclude NULL rows
-- This allows multiple NULL user_ids (one per system category)
DROP INDEX IF EXISTS idx_categories_user_id_name;
CREATE UNIQUE INDEX idx_categories_user_name_unique 
  ON public.categories(user_id, name) 
  WHERE user_id IS NOT NULL;

-- Step 3: Add CHECK constraint (enforce domain invariant)
-- Ensures NULL user_id can only be used for system categories
ALTER TABLE public.categories 
ADD CONSTRAINT chk_null_user_implies_system_default
  CHECK ((user_id IS NOT NULL) OR (is_system_default = true));

-- Step 4: Insert 24 system default categories with NULL user_id
-- Use ON CONFLICT to make migration idempotent
INSERT INTO public.categories (id, user_id, name, type, color, icon_name, is_system_default, created_at)
VALUES 
  (gen_random_uuid(), NULL, 'Salary', 'Income', '#27AE60', 'building-dollar', true, NOW()),
  (gen_random_uuid(), NULL, 'Sales', 'Income', '#27AE60', 'shopping-bag', true, NOW()),
  (gen_random_uuid(), NULL, 'Investments', 'Income', '#27AE60', 'trending-up', true, NOW()),
  (gen_random_uuid(), NULL, 'Gifts', 'Income', '#27AE60', 'gift', true, NOW()),
  (gen_random_uuid(), NULL, 'Other Income', 'Income', '#27AE60', 'coins', true, NOW()),
  (gen_random_uuid(), NULL, 'Housing', 'Expense', '#E74C3C', 'house', true, NOW()),
  (gen_random_uuid(), NULL, 'Utilities', 'Expense', '#E74C3C', 'lightbulb', true, NOW()),
  (gen_random_uuid(), NULL, 'Groceries', 'Expense', '#E74C3C', 'shopping-cart', true, NOW()),
  (gen_random_uuid(), NULL, 'Transportation', 'Expense', '#E74C3C', 'car', true, NOW()),
  (gen_random_uuid(), NULL, 'Entertainment', 'Expense', '#E74C3C', 'popcorn', true, NOW()),
  (gen_random_uuid(), NULL, 'Dining Out', 'Expense', '#E74C3C', 'utensils', true, NOW()),
  (gen_random_uuid(), NULL, 'Coffee', 'Expense', '#E74C3C', 'coffee', true, NOW()),
  (gen_random_uuid(), NULL, 'Subscription', 'Expense', '#E74C3C', 'bell', true, NOW()),
  (gen_random_uuid(), NULL, 'Insurance', 'Expense', '#E74C3C', 'shield', true, NOW()),
  (gen_random_uuid(), NULL, 'Healthcare', 'Expense', '#E74C3C', 'heart', true, NOW()),
  (gen_random_uuid(), NULL, 'Fitness', 'Expense', '#E74C3C', 'dumbbell', true, NOW()),
  (gen_random_uuid(), NULL, 'Education', 'Expense', '#E74C3C', 'book', true, NOW()),
  (gen_random_uuid(), NULL, 'Shopping', 'Expense', '#E74C3C', 'shopping-bag', true, NOW()),
  (gen_random_uuid(), NULL, 'Gas', 'Expense', '#E74C3C', 'gas-pump', true, NOW()),
  (gen_random_uuid(), NULL, 'Phone', 'Expense', '#E74C3C', 'phone', true, NOW()),
  (gen_random_uuid(), NULL, 'Internet', 'Expense', '#E74C3C', 'wifi', true, NOW()),
  (gen_random_uuid(), NULL, 'Hobbies', 'Expense', '#E74C3C', 'palette', true, NOW()),
  (gen_random_uuid(), NULL, 'Gifts Given', 'Expense', '#E74C3C', 'gift', true, NOW()),
  (gen_random_uuid(), NULL, 'Other Expense', 'Expense', '#E74C3C', 'dots-horizontal', true, NOW())
ON CONFLICT DO NOTHING;

-- Step 5: Add composite index for system category queries
-- Improves performance when filtering by is_system_default=true
CREATE INDEX idx_categories_is_system_default 
  ON public.categories(is_system_default, name)
  WHERE is_system_default = true;
