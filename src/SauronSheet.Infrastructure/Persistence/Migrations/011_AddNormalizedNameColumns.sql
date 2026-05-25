-- Migration: 011_AddNormalizedNameColumns
-- Purpose: Add normalized_name column to categories and subcategories for
--          diacritic-insensitive, case-insensitive deduplication in PDF import.
--
-- Design decision D3: Application-owned normalization (C# CategoryNormalizer).
-- DB stores the pre-computed key; queries use WHERE normalized_name = ?
--
-- BACKFILL NOTE: Initial backfill uses lower(trim(name)).
-- CategoryNormalizer (C#) also strips diacritics — divergence only if existing
-- category names contain diacritics (unlikely for the 4 current system defaults).
-- Task 1.5 integration test gates on divergence.
--
-- ROLLBACK: Remove columns and constraints; restore old unique indexes.

-- ============================================================
-- 1. Add columns (nullable for safe deployment)
-- ============================================================

ALTER TABLE public.categories
    ADD COLUMN IF NOT EXISTS normalized_name VARCHAR(50);

ALTER TABLE public.subcategories
    ADD COLUMN IF NOT EXISTS normalized_name VARCHAR(100);

-- ============================================================
-- 2. Backfill with lower(trim(name))
-- ============================================================

UPDATE public.categories
    SET normalized_name = lower(trim(name))
    WHERE normalized_name IS NULL;

UPDATE public.subcategories
    SET normalized_name = lower(trim(name))
    WHERE normalized_name IS NULL;

-- ============================================================
-- 3. Set NOT NULL
-- ============================================================

ALTER TABLE public.categories
    ALTER COLUMN normalized_name SET NOT NULL;

ALTER TABLE public.subcategories
    ALTER COLUMN normalized_name SET NOT NULL;

-- ============================================================
-- 4. Drop old unique constraints/indexes on (user_id, name)
-- ============================================================

-- Drop UNIQUE constraint from migration 002 (exact name verified from live schema)
ALTER TABLE public.categories
    DROP CONSTRAINT IF EXISTS categories_user_id_name_key;

-- Drop partial unique index from migration 007 (WHERE user_id IS NOT NULL)
DROP INDEX IF EXISTS public.idx_categories_user_name_unique;

-- Drop UNIQUE constraint on subcategories (exact name verified from live schema)
ALTER TABLE public.subcategories
    DROP CONSTRAINT IF EXISTS uq_subcategory_name;

-- ============================================================
-- 5. Add new unique constraints on normalized_name
--    Partial WHERE user_id IS NOT NULL: consistent with existing DB design
--    (system defaults have NULL user_id; NULLs would not conflict anyway,
--     but partial index makes the intent explicit).
-- ============================================================

CREATE UNIQUE INDEX IF NOT EXISTS uq_categories_user_normalized_name
    ON public.categories(user_id, normalized_name)
    WHERE user_id IS NOT NULL;

CREATE UNIQUE INDEX IF NOT EXISTS uq_subcategories_user_category_normalized_name
    ON public.subcategories(user_id, category_id, normalized_name)
    WHERE user_id IS NOT NULL;

-- ============================================================
-- 6. Performance indexes for normalized lookups
-- ============================================================

CREATE INDEX IF NOT EXISTS idx_categories_user_normalized
    ON public.categories(user_id, normalized_name);

CREATE INDEX IF NOT EXISTS idx_subcategories_user_cat_normalized
    ON public.subcategories(user_id, category_id, normalized_name);
