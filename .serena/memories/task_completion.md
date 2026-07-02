# Task Completion

Run these after every code change to verify correctness:

## Build
```bash
dotnet build
```

## Lint / Analyze
- .NET analyzers run as part of build (TreatWarningsAsErrors is on for Domain + Application)
- Review all warnings and errors; project must build clean

## Test
```bash
dotnet test
```
Check: all tests pass, no skipped/known-fail entries.

## E2E (if frontend changed)
```bash
npx playwright test
```
Must pass or have explicitly documented known failures.

## Git
```bash
git status
git diff --stat
```
Confirm only intended files changed, no secrets/keys committed.
