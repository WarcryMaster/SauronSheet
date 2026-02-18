-- Migration: 004_CreatePdfImportsTable.sql
-- Purpose: Metadata about imported PDF files
-- CRITICAL FIX I-2: Table name is pdf_imports (NOT import_batches)

CREATE TABLE IF NOT EXISTS public.pdf_imports (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL REFERENCES public.users(id) ON DELETE CASCADE,
    filename TEXT NOT NULL,
    imported_count INT NOT NULL DEFAULT 0,
    skipped_count INT NOT NULL DEFAULT 0,
    imported_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- Indexes
CREATE INDEX IF NOT EXISTS idx_pdf_imports_user ON public.pdf_imports(user_id);

-- Row Level Security
ALTER TABLE public.pdf_imports ENABLE ROW LEVEL SECURITY;

CREATE POLICY "Users can view own imports"
    ON public.pdf_imports FOR SELECT
    USING (auth.uid() = user_id);

CREATE POLICY "Users can insert own imports"
    ON public.pdf_imports FOR INSERT
    WITH CHECK (auth.uid() = user_id);


