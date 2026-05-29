-- Migration: Add balance column to transactions
-- Purpose: Store account balance at time of transaction for accurate duplicate detection.
-- Two transactions with same date/amount/description but different balances are NOT duplicates.

ALTER TABLE public.transactions
ADD COLUMN balance DECIMAL(15,2);

-- Existing rows get NULL balance (backward compatible)
