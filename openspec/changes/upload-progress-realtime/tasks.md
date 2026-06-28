# Tasks: Upload Progress Real-Time

## Review Workload Forecast

| Field | Value |
|-------|-------|
| Estimated changed lines | ~610 |
| 800-line budget risk | Low |
| Chained PRs recommended | Yes (force-chained per delivery strategy) |
| Suggested split | PR 1 → PR 2 → PR 3 |
| Delivery strategy | force-chained |
| Chain strategy | stacked-to-main |

Decision needed before apply: No
Chained PRs recommended: Yes
Chain strategy: stacked-to-main
400-line budget risk: Low

### Suggested Work Units

| Unit | Goal | PR | Notes |
|------|------|----|-------|
| 1 | Interface + tracker + handler progress | PR 1 | Foundation; backward compat; unit tests included |
| 2 | Endpoint + DI + partial view | PR 2 | Depends on PR 1; integration tests for UploadModel |
| 3 | Frontend progress UI + E2E tests | PR 3 | Depends on PR 2; E2E spec updates |

## Phase 1: Foundation — Interface + Progress Record

- [x] 1.1 Create `IImportProgressTracker` in `src/SauronSheet.Application/Services/IImportProgressTracker.cs` — methods: `InitializeAsync`, `ReportProgressAsync`, `CompleteAsync`, `FailAsync` per design contract
- [x] 1.2 Create `ImportProgress` record in `src/SauronSheet.Frontend/Services/ImportProgress.cs` — immutable record with UploadId, Filename, TotalRows, ProcessedRows, ImportedCount, SkippedCount, IsComplete, IsFailed, ErrorMessage, CurrentFileName, CurrentFileIndex, TotalFiles, UserId, StartedAt

## Phase 2: Progress Tracker Implementation

- [x] 2.1 Create `MemoryImportProgressTracker` in `src/SauronSheet.Frontend/Services/MemoryImportProgressTracker.cs` — `IMemoryCache`-backed (key: `import-progress-{uploadId}`, 5-min sliding expiration); expose `GetProgress(string uploadId)` for PageModel access
- [x] 2.2 Write unit tests in `tests/SauronSheet.Frontend.Tests/Services/MemoryImportProgressTrackerTests.cs` — cover: Initialize stores entry, ReportProgress updates counts, Complete sets IsComplete, Fail sets IsFailed+ErrorMessage, GetProgress returns null for unknown/expired IDs

## Phase 3: Handler Modification

- [x] 3.1 Add `string? UploadId = null` to `ImportTransactionsCommand` record in `src/SauronSheet.Application/Features/Transactions/Commands/ImportTransactionsCommand.cs`
- [x] 3.2 Inject `IImportProgressTracker?` (nullable) into `ImportTransactionsCommandHandler`; call `InitializeAsync` before row loop, `ReportProgressAsync` every N rows (1 if ≤100, 10 if >100), `CompleteAsync` after batch save, `FailAsync` in outer catch — all guarded by `request.UploadId is not null`
- [x] 3.3 Update `ImportTransactionsCommandHandlerTests` — existing tests pass unchanged (backward compat); add test: handler with tracker + UploadId calls InitializeAsync/CompleteAsync; add test: handler calls FailAsync on exception

## Phase 4: Backend Endpoint + DI

- [x] 4.1 Modify `Upload.cshtml.cs` — inject `IImportProgressTracker` (nullable default parameter to keep existing tests compiling); add `OnPostUploadAsync(CancellationToken)` returning `JsonResult(new { uploadId, success = true })` after generating GUID, initializing progress, validating files, and dispatching `ImportTransactionsCommand` with `UploadId`; add `OnGetProgress(string id)` returning inline Partial HTML with accessible progress bar; validate userId ownership from claims and return 403 for mismatch; set `HX-Trigger: {"stopPolling": true}` when `IsComplete` or `IsFailed`
- [x] 4.2 Progress partial — implemented inline within `OnGetProgress` (not as a separate view) per updated PR #2 scope; renders accessible progress bar (role="progressbar", aria-valuenow/min/max, aria-label) per REQ-PROG-011 and error state with alert-danger
- [x] 4.3 Register in `src/SauronSheet.Frontend/Program.cs`: `builder.Services.AddMemoryCache()` + `builder.Services.AddScoped<IImportProgressTracker, MemoryImportProgressTracker>()`
- [x] 4.4 Update `UploadModelTests` — constructor adjusted via optional tracker parameter; add tests: OnPostUploadAsync returns JSON with uploadId and initializes tracker, OnPostUploadAsync returns error JSON for empty/invalid files, OnGetProgress returns Partial HTML with progress bar, OnGetProgress returns 403 for mismatched userId, OnGetProgress sets HX-Trigger on completion

## Phase 5: Frontend — Progress UI + JS

- [ ] 5.1 Rewrite `Upload.cshtml` — add `#progress-container` + `#result-container` divs; extend Alpine.js `x-data` with `progress` state and `handleUpload(form)` using `fetch()` with FormData + antiforgery header; dispatch `startPolling` CustomEvent; wire HTMX polling (`hx-get`, `hx-trigger="every 1s"`, `hx-target="#progress-container"`) with `stopPolling` via `HX-Trigger` response header
- [ ] 5.2 Add error state: when response contains `IsFailed`, JS replaces progress bar with alert-danger and resets `uploading = false` to re-enable submit

## Phase 6: E2E Tests

- [ ] 6.1 Update `e2e/tests/02-upload-excel.spec.ts` — add test: progress bar appears with ARIA attributes during upload; add test: completion shows result summary with imported/skipped counts; add test: error state shows alert-danger on import failure
