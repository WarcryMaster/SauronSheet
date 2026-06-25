-- TZ-FIX: Correct ~225 existing transactions stored with wrong timezone.
-- These transactions were parsed as dd/MM/yyyy (no time) but stored with
-- Unspecified Kind, which PostgreSQL TIMESTAMPTZ interprets as local time
-- (Europe/Madrid, UTC+1/UTC+2). The hours 22:00 or 23:00 indicate the date
-- was shifted backward.
--
-- This migration reinterprets those timestamps as Europe/Madrid wall-clock
-- values and converts them back to UTC (midnight), fixing the date display.
BEGIN;

-- Ensure session timezone is UTC so the AT TIME ZONE conversion is deterministic
SET timezone = 'UTC';

UPDATE transactions
SET date = date AT TIME ZONE 'Europe/Madrid'
WHERE EXTRACT(HOUR FROM date) IN (22, 23);

COMMIT;
