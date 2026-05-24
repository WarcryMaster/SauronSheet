# Proposal: Category Selector Inline Create

## Intent

The Add Transaction page uses a `<select>` for categories. Users with many categories must scroll through a long list. Worse: if the category doesn't exist yet, they must abandon the form, create it, then return. We need a searchable input with inline creation — matching the ImportedFrom datalist pattern already in place.

## Scope

### In Scope
- Replace `<select>` with `<input list>` + `<datalist>` populated from existing categories
- InputModel: change `CategoryId` (Guid?) → `CategoryName` (string?)
- Resolve CategoryName → CategoryId in OnPostAsync: match existing by name, create via `CreateCategoryCommand` if new
- Remove `IsSystemDefault` references from view and `CategoryDto`
- Remove `CategoryDto.IsSystemDefault` field if no other consumers remain

### Out of Scope
- Subcategory selection (deferred — not yet in the add form)
- Searchable select for other pages (only Add Transaction)
- Multi-select categories

## Capabilities

### New Capabilities
- `category-inline-create`: inline category creation during transaction entry — user types a new name and it's created on submit

### Modified Capabilities
- `transactions`: the add transaction form now accepts `CategoryName` (string) instead of `CategoryId` (Guid?)

## Approach

Match the existing ImportedFrom pattern: `<input list="categories">` + `<datalist>`. On POST, the PageModel resolves the submitted name to an ID — match existing (case-insensitive) or create via `CreateCategoryCommand`. The `CreateTransactionCommand` stays untouched (still takes `Guid? CategoryId`).

## Affected Areas

| Area | Impact | Description |
|------|--------|-------------|
| `Frontend/Pages/Transactions/Add.cshtml` | Modified | Replace `<select>` with `<input>`+`<datalist>`, remove `IsSystemDefault` lock icon |
| `Frontend/Pages/Transactions/Add.cshtml.cs` | Modified | InputModel: `CategoryId`→`CategoryName`. OnPostAsync: resolve name→ID, call CreateCategoryCommand for new names |
| `Categories/DTOs/CategoryDto.cs` | Modified | Remove `IsSystemDefault` property and its usages |
| `Transactions/Commands/CreateTransactionCommand.cs` | Unchanged | Still takes `Guid? CategoryId` |
| `Categories/Commands/CreateCategoryCommand.cs` | Unchanged | Already exists, works as-is |

## Risks

| Risk | Likelihood | Mitigation |
|------|------------|------------|
| Race condition (two users create same category name simultaneously) | Low | CreateCategoryCommand handler validates uniqueness; second will throw DomainException (caught gracefully) |
| CategoryDto.IsSystemDefault removal breaks other consumers | Low | Search codebase after removal; only used in Add.cshtml and GetCategoriesQueryHandler |

## Rollback Plan

Revert the 4 modified files. The command layer was never changed, so the database is untouched. No migration needed.

## Dependencies

- `CreateCategoryCommand` and handler (already exists)
- `CreateTransactionCommand` (unchanged)

## Success Criteria

- [ ] User can type to filter categories in the Add Transaction form
- [ ] User can type a new category name and have it created on submit
- [ ] Empty category name → Uncategorized (no error)
- [ ] Existing category name matched case-insensitively
- [ ] No `IsSystemDefault` references remain in the add transaction code
- [ ] All existing tests still pass
