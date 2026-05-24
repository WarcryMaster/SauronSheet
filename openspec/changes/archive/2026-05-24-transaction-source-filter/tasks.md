# Tasks: Transaction Source Filter

Decision needed before apply: No
Chained PRs recommended: No
Chain strategy: single-pr
400-line budget risk: Low

## Review Workload Forecast

| Field | Value |
|-------|-------|
| Estimated changed lines | 80–120 |
| 400-line budget risk | Low |
| Chained PRs recommended | No |
| Suggested split | Single PR |
| Delivery strategy | single-pr |

## Phase 1: Domain Specification

- [x] 1.1 Create `src/SauronSheet.Domain/Specifications/TransactionByImportedFromSpecification.cs` — spec with `OrdinalIgnoreCase` matching on `t.ImportedFrom`. Null guard in expression body (no `.Equals()` on null). Test: case-insensitive match (RF-1a), null ImportedFrom not matched (RF-1b)

## Phase 2: Application Layer — Queries

- [x] 2.1 Modify `src/SauronSheet.Application/Features/Transactions/Queries/GetTransactionsQuery.cs` — add `string? ImportedFrom = null` as last parameter to the record
- [x] 2.2 Modify `src/SauronSheet.Application/Features/Transactions/Queries/GetTransactionsQueryHandler.cs` — compose `TransactionByImportedFromSpecification` conditionally after the date range block (same `CompositeSpecification.And` pattern as `CategoryId`). Test: filter active composes spec (RF-3a), null ImportedFrom skips spec (RF-3b)
- [x] 2.3 Create `src/SauronSheet.Application/Features/Transactions/Queries/GetDistinctImportedSourcesQuery.cs` — empty query record `IRequest<List<string>>`
- [x] 2.4 Create `src/SauronSheet.Application/Features/Transactions/Queries/GetDistinctImportedSourcesQueryHandler.cs` — fetch via `TransactionByUserSpecification`, LINQ: `Select → Where(not null/empty) → Distinct(OrdinalIgnoreCase) → OrderBy`. Test: distinct sources + alphabetical (RF-6a), empty list when no transactions (RF-6b)
- [x] 2.5 Verify `src/SauronSheet.Application/DependencyInjection.cs` — confirm MediatR auto-registration covers the new handler (no change needed if assembly scanning is in place)

## Phase 3: Frontend — PageModel

- [x] 3.1 Modify `src/SauronSheet.Frontend/Pages/Transactions/Index.cshtml.cs` — add `[BindProperty(SupportsGet = true)] public string? ImportedFrom` + `public List<string> AvailableSources`. In `OnGetAsync()`: load sources via `_mediator.Send(new GetDistinctImportedSourcesQuery())` before the existing query; pass `ImportedFrom` to `GetTransactionsQuery`

## Phase 4: Frontend — Razor View

- [x] 4.1 Modify `src/SauronSheet.Frontend/Pages/Transactions/Index.cshtml` — add `<div>` with `<input list="importedFromList">` + `<datalist id="importedFromList">` between the date range filter and the Apply button. Empty `<option value="">` first for clear. Use `form-control form-control-sm`. Bind value via `@Model.ImportedFrom`

## Phase 5: Build & Verify

- [x] 5.1 `dotnet build` — confirm no compilation errors
- [x] 5.2 `dotnet test` — confirm all existing + new tests pass
- [x] 5.3 Manual verification: load /transactions, verify datalist shows distinct sources, selecting one filters the table, clearing restores full list, type-to-filter narrows options
