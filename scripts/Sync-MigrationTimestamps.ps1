<#
.SYNOPSIS
    Synchronise local migration file timestamps with the remote Supabase database.

.DESCRIPTION
    Run this AFTER `supabase link` but BEFORE `supabase db push`.
    Queries the remote supabase_migrations.schema_migrations table, compares
    each migration's (name, version) pair against local files, and renames
    any local file whose timestamp (version) differs from the remote.

    Why this is needed:
    Migration filenames include a timestamp prefix (e.g. 20260626201918).
    If a migration file is regenerated locally (e.g. after rebase, merge
    conflict, or `supabase migration new`), the timestamp changes. The
    remote database still expects the old timestamp. This script bridges
    the gap automatically.

.PARAMETER MigrationsDir
    Path to the supabase/migrations directory. Default: supabase/migrations

.PARAMETER SupabaseExecutable
    Path to the supabase CLI. Default: supabase (assumes it is on PATH).

.EXAMPLE
    # After supabase link, before supabase db push
    ./scripts/Sync-MigrationTimestamps.ps1

.NOTES
    Exit code: 0 on success (even if nothing changed).
               1 on error (failed to query remote, failed to rename).
#>

param(
    [string]$MigrationsDir = "supabase/migrations",
    [string]$SupabaseExecutable = "supabase"
)

$ErrorActionPreference = "Stop"

# ── 1. Query remote migrations ───────────────────────────────────────────────
Write-Host ":: Fetching remote migration versions from supabase_migrations.schema_migrations ..."

try {
    $raw = & $SupabaseExecutable db query --linked --output json "SELECT version, name FROM supabase_migrations.schema_migrations ORDER BY version" 2>$null
    if (-not $raw) {
        throw "No output from supabase db query"
    }
    $remoteMigrations = $raw | ConvertFrom-Json
} catch {
    Write-Warning "Could not query remote migrations: $_"
    Write-Warning "Skipping timestamp sync. Ensure 'supabase link' has been run first."
    exit 1
}

# Build lookup: name -> version (remote)
$remoteByName = @{}
foreach ($m in $remoteMigrations) {
    $remoteByName[$m.name] = $m.version
}

Write-Host ":: Found $($remoteByName.Count) remote migration(s)."

# ── 2. Check local migration files ───────────────────────────────────────────
$migrationsPath = Resolve-Path $MigrationsDir -ErrorAction Stop
$changed = 0
$errors = 0

$localFiles = Get-ChildItem "$migrationsPath/*.sql" | Sort-Object Name
Write-Host ":: Checking $($localFiles.Count) local migration file(s) ..."

foreach ($file in $localFiles) {
    $baseName = $file.BaseName  # e.g. 20260626201918_backfill_subcategory_id_from_bank_subcategory

    if ($baseName -match '^(\d{14})_(.+)$') {
        $localVersion = $matches[1]
        $localName    = $matches[2]

        if ($remoteByName.ContainsKey($localName)) {
            $remoteVersion = $remoteByName[$localName]

            if ($localVersion -ne $remoteVersion) {
                $newName = "${remoteVersion}_${localName}$($file.Extension)"
                Write-Host "  RENAMING: $($file.Name) -> $newName"
                try {
                    Rename-Item -Path $file.FullName -NewName $newName -ErrorAction Stop
                    $changed++
                } catch {
                    Write-Error "  FAILED to rename $($file.Name): $_"
                    $errors++
                }
            } else {
                Write-Host "  OK: $($file.Name)"
            }
        } else {
            Write-Host "  NEW (not in remote): $($file.Name)"
        }
    } else {
        Write-Warning "  SKIPPED (unexpected filename format): $($file.Name)"
    }
}

# ── 3. Summary ───────────────────────────────────────────────────────────────
if ($changed -gt 0) {
    Write-Host ":: Fixed $changed migration file(s) with mismatched timestamps."
} else {
    Write-Host ":: All migration timestamps match remote. No changes needed."
}

if ($errors -gt 0) {
    Write-Error ":: $errors error(s) during renaming."
    exit 1
}

exit 0
