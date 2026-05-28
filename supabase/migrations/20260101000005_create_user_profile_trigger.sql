-- Migration: 005_CreateUserProfileTrigger.sql
-- Purpose: Auto-create user profile in public.users when a new user signs up via Supabase Auth
-- Dependency: 001_CreateUsersTable.sql must be executed first

-- Function: Inserts a row into public.users when auth.users gets a new record
CREATE OR REPLACE FUNCTION public.handle_new_user()
RETURNS trigger AS $$
BEGIN
    INSERT INTO public.users (id, email, created_at)
    VALUES (NEW.id, NEW.email, NOW())
    ON CONFLICT (id) DO NOTHING;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql SECURITY DEFINER;

-- Trigger: Fires after each new signup in Supabase Auth
CREATE OR REPLACE TRIGGER on_auth_user_created
    AFTER INSERT ON auth.users
    FOR EACH ROW
    EXECUTE FUNCTION public.handle_new_user();
