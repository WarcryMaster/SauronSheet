# Suggested Commands

## Build / Run
- `dotnet build` — full solution build
- `dotnet run --project src/SauronSheet.Frontend/` — run dev server (default https://localhost:7000)

## Test (Windows PowerShell 7+)
- `dotnet test` — all tests (results in test-results/dotnet)
- `dotnet test tests/SauronSheet.Domain.Tests/` — domain unit tests only
- `dotnet test tests/SauronSheet.Application.Tests/` — application tests only
- `npx playwright test` — E2E tests (from project root)

## Supabase
- `scripts/Sync-MigrationTimestamps.ps1` — sync migration timestamps after supabase link
- `supabase db push` — push local migrations to linked project
- `supabase start` — local Supabase stack

## Windows-specific
- `pwsh` or `powershell` for PowerShell 7+
- `Get-ChildItem` instead of `ls`
- `Set-Content` / `Add-Content` for file writes (though prefer AI tools)
