-- Rollback Feature 3: System Categories Global Scope Refactoring
-- Reverts database to pre-Feature 3 state
-- Safe: Only deletes system categories (NULL user_id), preserves user categories

-- Step 1: Delete system categories (NULL user_id)
DELETE FROM public.categories 
WHERE user_id IS NULL AND is_system_default = true;

-- Step 2: Remove new indexes
DROP INDEX IF EXISTS public.idx_categories_is_system_default;
DROP INDEX IF EXISTS public.idx_categories_user_name_unique;

-- Step 3: Drop CHECK constraint
ALTER TABLE public.categories 
DROP CONSTRAINT IF EXISTS chk_null_user_implies_system_default;

-- Step 4: Recreate old UNIQUE index (user_id, name)
-- Assumes user_id is NOT NULL for user categories
CREATE UNIQUE INDEX idx_categories_user_id_name 
ON public.categories(user_id, name);

-- Step 5: Make user_id NOT NULL again
-- Safe: System categories already deleted in Step 1
ALTER TABLE public.categories 
ALTER COLUMN user_id SET NOT NULL;
