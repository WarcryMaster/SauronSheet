-- Migration: 010_EnableRLSOnSubcategoriesAndBankTranslations
-- Purpose: Enable Row Level Security on subcategories and bank_category_translations
-- These tables were created without RLS, exposing data to anyone with the anon key

-- ============================================================
-- public.subcategories
-- ============================================================

ALTER TABLE public.subcategories ENABLE ROW LEVEL SECURITY;

-- Users can view their own subcategories AND system-defined defaults
CREATE POLICY "Users can view own and system subcategories"
    ON public.subcategories FOR SELECT
    USING (auth.uid() = user_id OR is_system_default = true);

-- Users can create subcategories for themselves (never system defaults)
CREATE POLICY "Users can insert own subcategories"
    ON public.subcategories FOR INSERT
    WITH CHECK (auth.uid() = user_id AND is_system_default = false);

-- Users can update their own non-system subcategories
CREATE POLICY "Users can update own subcategories"
    ON public.subcategories FOR UPDATE
    USING (auth.uid() = user_id AND is_system_default = false)
    WITH CHECK (auth.uid() = user_id AND is_system_default = false);

-- Users can delete their own non-system subcategories
CREATE POLICY "Users can delete own subcategories"
    ON public.subcategories FOR DELETE
    USING (auth.uid() = user_id AND is_system_default = false);

COMMENT ON TABLE public.subcategories IS 'Subcategories linked to categories. RLS: user_id or is_system_default';

-- ============================================================
-- public.bank_category_translations
-- ============================================================

ALTER TABLE public.bank_category_translations ENABLE ROW LEVEL SECURITY;

-- Any authenticated user can read bank category translations (reference data)
-- INSERT/UPDATE/DELETE are intentionally not exposed — only service_role can modify
CREATE POLICY "Authenticated users can view bank category translations"
    ON public.bank_category_translations FOR SELECT
    USING (auth.role() = 'authenticated');

COMMENT ON TABLE public.bank_category_translations IS 'Bank category to internal mapping. RLS: authenticated users can SELECT only. Modifications via service_role.';
