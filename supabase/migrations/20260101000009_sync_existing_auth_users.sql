-- Migration: 009_SyncExistingAuthUsers.sql
-- Sincroniza usuarios existentes de auth.users a public.users (para usuarios previos al trigger)
-- Ejecutar una sola vez tras desplegar el trigger de perfil

INSERT INTO public.users (id, email, created_at)
SELECT id, email, NOW()
FROM auth.users
WHERE id NOT IN (SELECT id FROM public.users);
