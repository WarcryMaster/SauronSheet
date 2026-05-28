-- Migration: 002_CreateCategoriesTable.sql
-- Purpose: Expense categories (system defaults + user-defined)

-- Create categories table if not exists
CREATE TABLE IF NOT EXISTS public.categories (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL REFERENCES public.users(id) ON DELETE CASCADE,
    name VARCHAR(50) NOT NULL,
    type VARCHAR(10) NOT NULL CHECK (type IN ('Income', 'Expense')),
    color VARCHAR(7) NOT NULL,
    icon_name VARCHAR(100) NOT NULL,
    is_system_default BOOLEAN NOT NULL DEFAULT FALSE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW(),
    UNIQUE(user_id, name)
);

-- Create trigger function for updated_at if not exists
CREATE OR REPLACE FUNCTION update_updated_at_column()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = CURRENT_TIMESTAMP;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- Create trigger for categories table
DROP TRIGGER IF EXISTS categories_updated_at_trigger ON public.categories;
CREATE TRIGGER categories_updated_at_trigger
BEFORE UPDATE ON public.categories
FOR EACH ROW
EXECUTE FUNCTION update_updated_at_column();

-- Indexes
CREATE INDEX IF NOT EXISTS idx_categories_user ON public.categories(user_id);
CREATE INDEX IF NOT EXISTS idx_categories_user_name ON public.categories(user_id, name);
CREATE INDEX IF NOT EXISTS idx_categories_user_system ON public.categories(user_id, is_system_default);

-- Row Level Security
ALTER TABLE public.categories ENABLE ROW LEVEL SECURITY;

-- RLS policies - system categories visible to all, personal categories visible to owner only
CREATE POLICY "Users can view own and system categories"
    ON public.categories FOR SELECT
    USING (auth.uid() = user_id OR is_system_default = true);

CREATE POLICY "Users can insert own categories"
    ON public.categories FOR INSERT
    WITH CHECK (auth.uid() = user_id AND is_system_default = false);

CREATE POLICY "Users can update own categories"
    ON public.categories FOR UPDATE
    USING (auth.uid() = user_id AND is_system_default = false)
    WITH CHECK (auth.uid() = user_id AND is_system_default = false);

CREATE POLICY "Users can delete own categories"
    ON public.categories FOR DELETE
    USING (auth.uid() = user_id AND is_system_default = false);

-- NOTE: System default categories are inserted in Feature 3 migration (007_SystemCategoriesGlobalScope.sql)
-- which makes user_id nullable and adds 24 system categories with NULL user_id
