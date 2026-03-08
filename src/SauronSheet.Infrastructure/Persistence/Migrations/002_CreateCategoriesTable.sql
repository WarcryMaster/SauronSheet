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

-- Insert 24 system default categories (use sys_admin userid as placeholder)
-- Income (5)
INSERT INTO public.categories (user_id, name, type, color, icon_name, is_system_default) 
VALUES ('00000000-0000-0000-0000-000000000000', 'Salary', 'Income', '#27AE60', 'building-dollar', true) 
ON CONFLICT DO NOTHING;

INSERT INTO public.categories (user_id, name, type, color, icon_name, is_system_default) 
VALUES ('00000000-0000-0000-0000-000000000000', 'Sales', 'Income', '#27AE60', 'storefront', true) 
ON CONFLICT DO NOTHING;

INSERT INTO public.categories (user_id, name, type, color, icon_name, is_system_default) 
VALUES ('00000000-0000-0000-0000-000000000000', 'Investments', 'Income', '#27AE60', 'trending-up', true) 
ON CONFLICT DO NOTHING;

INSERT INTO public.categories (user_id, name, type, color, icon_name, is_system_default) 
VALUES ('00000000-0000-0000-0000-000000000000', 'Gifts', 'Income', '#27AE60', 'gift', true) 
ON CONFLICT DO NOTHING;

INSERT INTO public.categories (user_id, name, type, color, icon_name, is_system_default) 
VALUES ('00000000-0000-0000-0000-000000000000', 'Other Income', 'Income', '#27AE60', 'inbox-in', true) 
ON CONFLICT DO NOTHING;

-- Fixed Expenses (5)
INSERT INTO public.categories (user_id, name, type, color, icon_name, is_system_default) 
VALUES ('00000000-0000-0000-0000-000000000000', 'Housing', 'Expense', '#E74C3C', 'home', true) 
ON CONFLICT DO NOTHING;

INSERT INTO public.categories (user_id, name, type, color, icon_name, is_system_default) 
VALUES ('00000000-0000-0000-0000-000000000000', 'Utilities', 'Expense', '#E74C3C', 'lightning-bolt', true) 
ON CONFLICT DO NOTHING;

INSERT INTO public.categories (user_id, name, type, color, icon_name, is_system_default) 
VALUES ('00000000-0000-0000-0000-000000000000', 'Insurance', 'Expense', '#E74C3C', 'shield-check', true) 
ON CONFLICT DO NOTHING;

INSERT INTO public.categories (user_id, name, type, color, icon_name, is_system_default) 
VALUES ('00000000-0000-0000-0000-000000000000', 'Subscriptions', 'Expense', '#E74C3C', 'ticket', true) 
ON CONFLICT DO NOTHING;

INSERT INTO public.categories (user_id, name, type, color, icon_name, is_system_default) 
VALUES ('00000000-0000-0000-0000-000000000000', 'Education', 'Expense', '#E74C3C', 'graduation-cap', true) 
ON CONFLICT DO NOTHING;

-- Variable Expenses (5)
INSERT INTO public.categories (user_id, name, type, color, icon_name, is_system_default) 
VALUES ('00000000-0000-0000-0000-000000000000', 'Groceries', 'Expense', '#F39C12', 'basket', true) 
ON CONFLICT DO NOTHING;

INSERT INTO public.categories (user_id, name, type, color, icon_name, is_system_default) 
VALUES ('00000000-0000-0000-0000-000000000000', 'Transportation', 'Expense', '#F39C12', 'car', true) 
ON CONFLICT DO NOTHING;

INSERT INTO public.categories (user_id, name, type, color, icon_name, is_system_default) 
VALUES ('00000000-0000-0000-0000-000000000000', 'Personal Care', 'Expense', '#F39C12', 'soap', true) 
ON CONFLICT DO NOTHING;

INSERT INTO public.categories (user_id, name, type, color, icon_name, is_system_default) 
VALUES ('00000000-0000-0000-0000-000000000000', 'Home Maintenance', 'Expense', '#F39C12', 'hammer-wrench', true) 
ON CONFLICT DO NOTHING;

INSERT INTO public.categories (user_id, name, type, color, icon_name, is_system_default) 
VALUES ('00000000-0000-0000-0000-000000000000', 'Pets', 'Expense', '#F39C12', 'paw', true) 
ON CONFLICT DO NOTHING;

-- Lifestyle (5)
INSERT INTO public.categories (user_id, name, type, color, icon_name, is_system_default) 
VALUES ('00000000-0000-0000-0000-000000000000', 'Restaurants', 'Expense', '#9B59B6', 'utensils', true) 
ON CONFLICT DO NOTHING;

INSERT INTO public.categories (user_id, name, type, color, icon_name, is_system_default) 
VALUES ('00000000-0000-0000-0000-000000000000', 'Entertainment', 'Expense', '#9B59B6', 'film', true) 
ON CONFLICT DO NOTHING;

INSERT INTO public.categories (user_id, name, type, color, icon_name, is_system_default) 
VALUES ('00000000-0000-0000-0000-000000000000', 'Shopping', 'Expense', '#9B59B6', 'shopping-bag', true) 
ON CONFLICT DO NOTHING;

INSERT INTO public.categories (user_id, name, type, color, icon_name, is_system_default) 
VALUES ('00000000-0000-0000-0000-000000000000', 'Travel', 'Expense', '#9B59B6', 'airplane', true) 
ON CONFLICT DO NOTHING;

INSERT INTO public.categories (user_id, name, type, color, icon_name, is_system_default) 
VALUES ('00000000-0000-0000-0000-000000000000', 'Health & Wellness', 'Expense', '#9B59B6', 'heart', true) 
ON CONFLICT DO NOTHING;

-- Finance & Other (4)
INSERT INTO public.categories (user_id, name, type, color, icon_name, is_system_default) 
VALUES ('00000000-0000-0000-0000-000000000000', 'Debt Payments', 'Expense', '#3498DB', 'credit-card', true) 
ON CONFLICT DO NOTHING;

INSERT INTO public.categories (user_id, name, type, color, icon_name, is_system_default) 
VALUES ('00000000-0000-0000-0000-000000000000', 'Savings & Investment', 'Expense', '#3498DB', 'piggy-bank', true) 
ON CONFLICT DO NOTHING;

INSERT INTO public.categories (user_id, name, type, color, icon_name, is_system_default) 
VALUES ('00000000-0000-0000-0000-000000000000', 'Donations', 'Expense', '#3498DB', 'hand-heart', true) 
ON CONFLICT DO NOTHING;

INSERT INTO public.categories (user_id, name, type, color, icon_name, is_system_default) 
VALUES ('00000000-0000-0000-0000-000000000000', 'Unexpected Expenses', 'Expense', '#3498DB', 'exclamation-triangle', true) 
ON CONFLICT DO NOTHING;
