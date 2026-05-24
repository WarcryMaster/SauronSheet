# Tasks: Category Selector Inline Create

## Review Workload Forecast

| Field | Value |
|-------|-------|
| Estimated changed lines | ~90-110 |
| 400-line budget risk | Low |
| Chained PRs recommended | No |
| Suggested split | single PR |
| Delivery strategy | single-pr |

Decision needed before apply: No
Chained PRs recommended: No
Chain strategy: size-exception
400-line budget risk: Low

## Phase 1: Foundation (DTO Cleanup)

- [x] 1.1 Remove `IsSystemDefault` property from `CategoryDto.cs`
- [x] 1.2 Remove `c.IsSystemDefault` reference in `GetCategoriesQueryHandler.cs` Select call

## Phase 2: PageModel Changes

- [x] 2.1 Change `AddTransactionInputModel.CategoryId` (Guid?) → `CategoryName` (string?) with `[StringLength(500)]`
- [x] 2.2 Add `ResolveCategoryIdAsync(string? name)` internal method in AddModel with match/create/null logic
- [x] 2.3 Wire `ResolveCategoryIdAsync` into `OnPostAsync` before sending `CreateTransactionCommand`

## Phase 3: View Changes

- [x] 3.1 Replace `<select>` with `<input list="categories">` + `<datalist id="categories">` in `Add.cshtml`
- [x] 3.2 Remove `IsSystemDefault` lock icon rendering from the datalist options
- [x] 3.3 Update label/placeholder to indicate free-text entry

## Phase 4: Testing & Verification

- [x] 4.1 Write unit tests for `ResolveCategoryIdAsync` (match, new, null, case-insensitive)
- [x] 4.2 Verify build: `dotnet build` with TreatWarningsAsErrors passes
- [x] 4.3 Run full test suite: `dotnet test`
