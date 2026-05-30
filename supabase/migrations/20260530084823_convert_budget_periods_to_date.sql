-- Migration: convert_budget_periods_to_date
-- Purpose: Convert period_start and period_end from TIMESTAMPTZ to DATE
-- to eliminate timezone-induced month drift in budget periods.
--
-- Root cause: TIMESTAMPTZ columns could drift a midnight UTC boundary
-- (e.g. 2026-04-30 22:00:00+00 stored for a May 2026 budget created in UTC+2)
-- causing month comparisons to land on the wrong month.
--
-- Strategy:
--   1. Normalise existing data by interpreting timestamps in the Europe/Madrid
--      timezone (where drifted records were produced), truncating to month start,
--      and recomputing period_end as the last day of that same month.
--   2. Drop the dependent unique constraint and index (both reference period_start).
--   3. Alter the column types to DATE.
--   4. Re-create the constraint and index on the DATE type.

BEGIN;

-- ── Step 1: Normalise any drifted rows ───────────────────────────────────────
-- Interpret the stored TIMESTAMPTZ in Europe/Madrid so that, for example,
-- '2026-04-30 22:00:00+00' (= midnight CEST on 2026-05-01) becomes 2026-05-01.
-- period_start is then forced to the first day of the resulting month,
-- period_end is derived as the last day of that same month.
UPDATE public.budgets
SET
    period_start = date_trunc(
        'month',
        period_start AT TIME ZONE 'Europe/Madrid'
    )::timestamptz,
    period_end   = (
        date_trunc('month', period_start AT TIME ZONE 'Europe/Madrid')
        + INTERVAL '1 month'
        - INTERVAL '1 day'
    )::timestamptz;

-- ── Step 2: Drop unique constraint that references period_start ───────────────
-- The constraint was created without an explicit name, so Postgres auto-named it.
ALTER TABLE public.budgets
    DROP CONSTRAINT IF EXISTS budgets_user_id_category_id_period_start_key;

-- ── Step 3: Drop index that references period_start ──────────────────────────
DROP INDEX IF EXISTS idx_budgets_user_period;

-- ── Step 4: Alter column types from TIMESTAMPTZ to DATE ──────────────────────
-- After step 1 all stored timestamps are already UTC midnight on a month
-- boundary, so ::date is a safe, lossless cast.
ALTER TABLE public.budgets
    ALTER COLUMN period_start TYPE DATE USING period_start::date;

ALTER TABLE public.budgets
    ALTER COLUMN period_end TYPE DATE USING period_end::date;

-- ── Step 5: Re-create unique constraint on the DATE type ─────────────────────
ALTER TABLE public.budgets
    ADD CONSTRAINT budgets_user_id_category_id_period_start_key
    UNIQUE (user_id, category_id, period_start);

-- ── Step 6: Re-create index on the DATE type ─────────────────────────────────
CREATE INDEX IF NOT EXISTS idx_budgets_user_period
    ON public.budgets (user_id, period_start);

COMMIT;
