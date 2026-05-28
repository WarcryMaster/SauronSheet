# Tasks: supabase-cli-migrations

## Phase 1: Infrastructure CLI Setup + Migration Renaming (PR 1)

- [x] 1.1 Create `supabase/config.toml` at project root with correct Supabase project configuration
- [x] 1.2 Create `supabase/migrations/` directory
- [x] 1.3 Move and rename all 12 existing migration files from `src/SauronSheet.Infrastructure/Persistence/Migrations/` to `supabase/migrations/` with CLI naming format (`YYYYMMDDHHMMSS_descriptive_name.sql`)
- [x] 1.4 Delete the old `src/SauronSheet.Infrastructure/Persistence/Migrations/` directory
- [x] 1.5 Update `.gitignore` to exclude `supabase/.temp/`
- [x] 1.6 Verify: all 12 files moved, old dir removed, config valid, `dotnet build` passes

## Phase 2: CI/CD + README Updates (PR 2 — NOT IN SCOPE)

- [x] 2.1 Update CI/CD workflow for Supabase CLI migrations
- [x] 2.2 Update README with new migration workflow
