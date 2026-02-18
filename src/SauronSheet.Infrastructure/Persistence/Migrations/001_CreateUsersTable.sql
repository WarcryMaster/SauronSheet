-- Migration: 001_CreateUsersTable.sql
-- Purpose: User profile table (Supabase Auth manages auth.users)
-- NOTE: This migration should have been in Phase 1, but is added here as prerequisite

CREATE TABLE IF NOT EXISTS public.users (
    id UUID PRIMARY KEY REFERENCES auth.users(id) ON DELETE CASCADE,
    email TEXT NOT NULL UNIQUE,
    display_name TEXT,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ
);

-- Indexes
CREATE INDEX IF NOT EXISTS idx_users_email ON public.users(email);

-- Row Level Security
ALTER TABLE public.users ENABLE ROW LEVEL SECURITY;

CREATE POLICY "Users can view own profile"
    ON public.users FOR SELECT
    USING (auth.uid() = id);

CREATE POLICY "Users can update own profile"
    ON public.users FOR UPDATE
    USING (auth.uid() = id)
    WITH CHECK (auth.uid() = id);

COMMENT ON TABLE public.users IS 'User profiles linked to Supabase Auth';
COMMENT ON COLUMN public.users.id IS 'Foreign key to auth.users(id)';
