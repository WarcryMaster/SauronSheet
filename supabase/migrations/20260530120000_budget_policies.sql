-- Migration: budget_policies
-- Purpose: Redesign budgets table for permanent policy model with configurable period granularity.
-- Replaces the old monthly-budget schema (period_start/period_end TIMESTAMPTZ) with
-- an effective-date range model (effective_from/effective_until DATE) that supports
-- Monthly, Quarterly, Semester, and Annual granularities.
--
-- Strategy: Drop + recreate. No data preservation needed (confirmed by maintainer).
-- Old monthly budgets are prescindible.

BEGIN;

-- ── Step 1: Drop old table ───────────────────────────────────────────────────
DROP TABLE IF EXISTS public.budgets CASCADE;

-- ── Step 2: Ensure btree_gist extension for exclusion constraint ──────────────
CREATE EXTENSION IF NOT EXISTS btree_gist;

-- ── Step 3: Create new budgets table ─────────────────────────────────────────
CREATE TABLE public.budgets (
    id UUID PRIMARY KEY,
    user_id UUID NOT NULL REFERENCES public.users(id) ON DELETE CASCADE,
    category_id UUID NOT NULL REFERENCES public.categories(id) ON DELETE CASCADE,
    effective_from DATE NOT NULL,
    effective_until DATE,
    period_granularity VARCHAR(10) NOT NULL DEFAULT 'Monthly'
        CHECK (period_granularity IN ('Monthly', 'Quarterly', 'Semester', 'Annual')),
    limit_amount DECIMAL(18, 2) NOT NULL CHECK (limit_amount > 0),
    currency VARCHAR(3) NOT NULL DEFAULT 'EUR',
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ,

    -- Ensure effective_until is on or after effective_from (when not null)
    CONSTRAINT chk_effective_dates CHECK (
        effective_until IS NULL OR effective_until >= effective_from
    )
);

-- ── Step 4: Exclusion constraint — no overlapping ranges per user+category ────
-- Uses btree_gist to index scalar columns (user_id, category_id) alongside
-- a daterange for overlap detection.
-- A NULL effective_until is treated as the infinite future (9999-12-31)
-- so that a permanent budget blocks any overlapping budget.
ALTER TABLE public.budgets
    ADD CONSTRAINT budgets_no_overlap
    EXCLUDE USING gist (
        user_id WITH =,
        category_id WITH =,
        daterange(
            effective_from,
            COALESCE(effective_until, '9999-12-31'::date),
            '[]'
        ) WITH &&
    );

-- ── Step 5: Indexes ──────────────────────────────────────────────────────────
CREATE INDEX idx_budgets_user ON public.budgets (user_id);
CREATE INDEX idx_budgets_user_category ON public.budgets (user_id, category_id);
CREATE INDEX idx_budgets_effective ON public.budgets (effective_from, effective_until);

-- ── Step 6: Row Level Security ───────────────────────────────────────────────
ALTER TABLE public.budgets ENABLE ROW LEVEL SECURITY;

CREATE POLICY "Users can view own budgets"
    ON public.budgets FOR SELECT
    TO authenticated
    USING ((SELECT auth.uid()) = user_id);

CREATE POLICY "Users can insert own budgets"
    ON public.budgets FOR INSERT
    TO authenticated
    WITH CHECK ((SELECT auth.uid()) = user_id);

CREATE POLICY "Users can update own budgets"
    ON public.budgets FOR UPDATE
    TO authenticated
    USING ((SELECT auth.uid()) = user_id)
    WITH CHECK ((SELECT auth.uid()) = user_id);

CREATE POLICY "Users can delete own budgets"
    ON public.budgets FOR DELETE
    TO authenticated
    USING ((SELECT auth.uid()) = user_id);

COMMIT;
