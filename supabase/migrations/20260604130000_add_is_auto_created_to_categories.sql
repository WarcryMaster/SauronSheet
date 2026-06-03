-- Purpose: Add is_auto_created column to categories to distinguish
-- user-created categories from those auto-created during Excel import.

ALTER TABLE public.categories
    ADD COLUMN IF NOT EXISTS is_auto_created BOOLEAN NOT NULL DEFAULT false;
