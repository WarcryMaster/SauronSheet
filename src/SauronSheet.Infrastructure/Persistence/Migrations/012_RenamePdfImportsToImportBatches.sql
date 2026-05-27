-- Migration: 012_RenamePdfImportsToImportBatches.sql
-- Purpose: Rename pdf_imports → import_batches for neutral naming (format-agnostic).
--          Preserves all data, indexes, RLS policies, and FK constraints in-place.
-- Rollback: ALTER TABLE public.import_batches RENAME TO pdf_imports;

-- Rename the table
ALTER TABLE IF EXISTS public.pdf_imports RENAME TO import_batches;

-- Rename the existing user index to match the new table name
ALTER INDEX IF EXISTS idx_pdf_imports_user RENAME TO idx_import_batches_user;

-- Drop and recreate RLS policies under neutral names.
-- (Policies are bound to the table name; renaming the table preserves them
--  but with the old names — replace for clarity.)
DROP POLICY IF EXISTS "Users can view own imports" ON public.import_batches;
DROP POLICY IF EXISTS "Users can insert own imports" ON public.import_batches;

CREATE POLICY "Users can view own import_batches"
    ON public.import_batches FOR SELECT
    USING (auth.uid() = user_id);

CREATE POLICY "Users can insert own import_batches"
    ON public.import_batches FOR INSERT
    WITH CHECK (auth.uid() = user_id);
