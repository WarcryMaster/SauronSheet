# Proposal: Bank Category Resolution

## Intent

ING PDF parser extracts Spanish bank category/subcategory (e.g., "Compras" / "Ropa y complementos"), but the C# layer is blind to all of it — `Transaction` entity, `TransactionRow` DTO, `TransactionDto`, and the import handler all ignore these fields. Result: all 569 imported transactions land uncategorized despite the DB already having `subcategories` (13 rows), `bank_category_translations` (15 rows), and bank-category columns on `transactions`.

## Scope

### In Scope
- `CategorySource` enum + `SubcategoryId`, `BankCategoryName`, `BankSubcategoryName` value objects
- Subcategory entity and repository infrastructure
- Resolution service: normalizes bank values → checks `bank_category_translations` → name-matches user categories → assigns subcategory
- Import handler integration: save raw values, run resolution, set `category_source`
- DB migration: remove system default categories (English ones), add missing columns to transactions
- Transaction DTO/row updates for all new fields
- `category_source` values: `Legacy`, `RawOnly`, `AutoMatched`, `UserOverride`

### Out of Scope
- Frontend UI for bank_category_translations management
- Batch recategorization UI
- Subcategory management UI
- Data migration for 569 existing transactions (raw values already present in DB)

## Capabilities

> No existing `openspec/specs/` — all new capabilities.

### New Capabilities
- `category-resolution`: Domain service, value objects, and handler logic for auto-matching bank categories to system categories
- `subcategory-management`: Subcategory entity, repository, and relationship to categories/transactions

### Modified Capabilities
- None — this is entirely new functionality

## Approach

**User Categories + Name Match + Optional Translation Override:**

1. Remove all system default categories — only user-created categories exist
2. Import handler: save raw `bank_category`/`bank_subcategory` from `RawTransactionRow` always, then run resolution
3. Resolution: normalise → check `bank_category_translations` for override → name-match user's Category (case-insensitive) → if found, assign `CategoryId`, set `category_source='AutoMatched'` → if subcategory exists in same category, assign `SubcategoryId`
4. No match → leave `CategoryId=null`, `category_source='RawOnly'`
5. System never auto-creates categories or uses AI

## Affected Areas

| Area | Impact | Description |
|------|--------|-------------|
| `Domain/Entities/Transaction.cs` | Modified | Add bank_category, bank_subcategory, subcategory_id, category_source |
| `Domain/Entities/Subcategory.cs` | New | Subcategory entity (category FK, name, is_system_default) |
| `Domain/ValueObjects/CategorySource.cs` | New | Enum: Legacy, RawOnly, AutoMatched, UserOverride |
| `Domain/Services/ICategoryResolutionService.cs` | New | Resolution contract |
| `Domain/Repositories/ISubcategoryRepository.cs` | New | Subcategory persistence contract |
| `Application/Commands/ImportTransactionsFromPdfCommandHandler.cs` | Modified | Integrate resolution after parse |
| `Application/DTOs/TransactionDto.cs` | Modified | Add new fields |
| `Infrastructure/Persistence/TransactionRow.cs` | Modified | Add bank columns |
| `Infrastructure/Persistence/SupabaseTransactionRepository.cs` | Modified | Include subcategory in mapping |
| `Infrastructure/Persistence/SupabaseSubcategoryRepository.cs` | New | Subcategory + bank_category_translations read |
| `Infrastructure/Services/CategoryResolutionService.cs` | New | Resolution implementation |
| `Infrastructure/Persistence/Migrations/` | New | Remove system defaults, add missing columns |
| `Domain/Services/CategoryService.cs` | Modified | Remove system default seeding |

## Risks

| Risk | Likelihood | Mitigation |
|------|------------|------------|
| RLS: bank_category_translations read-only for authenticated | Low | Resolution service uses SELECT via Postgrest (matching existing RLS policy) |
| Removing system defaults breaks existing FK references | High | All 569 existing transactions have `category_id=null` — no FK references to system default categories. Safe to delete. |
| Postgrest OR limitation | Med | Use two separate queries (user categories + system defaults for legacy rows) merged in-memory — established pattern |

## Rollback Plan

1. Revert migration (restore system defaults from backup)
2. Remove new columns from `TransactionRow` mapping
3. Revert `Transaction` entity to original state
4. No data loss: raw bank values remain in DB, all transactions keep their current `category_id`

## Dependencies

- None external. Entirely within existing project stack.

## Success Criteria

- [ ] Import handler saves `bank_category`/`bank_subcategory` on every imported transaction
- [ ] ING "Compras"/"Ropa y complementos" resolves to Category "Compras" + Subcategory "Ropa y complementos" (if user created them)
- [ ] Unknown bank categories set `category_source='RawOnly'` with null `CategoryId`
- [ ] All system default categories removed (only user-created categories remain)
- [ ] 100% of existing tests pass unchanged
