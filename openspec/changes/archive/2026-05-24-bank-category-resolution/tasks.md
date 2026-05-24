# Tasks: Bank Category Resolution

Decision needed before apply: No
Chained PRs recommended: No
Chain strategy: single-pr
400-line budget risk: Low

## Review Workload Forecast

| Field | Value |
|-------|-------|
| Estimated changed lines | 900–1100 |
| 400-line budget risk | Low |
| Chained PRs recommended | No |
| Suggested split | Single PR (`size:exception` accepted) |
| Delivery strategy | single-pr |

## Phase 1: Foundation (Domain)

- [x] 1.1 Create `Domain/ValueObjects/CategorySource.cs` — enum Legacy|RawOnly|AutoMatched|UserOverride. Test: serialization
- [x] 1.2 Create `Domain/ValueObjects/SubcategoryId.cs` — strong-typed Guid. Test: creation
- [x] 1.3 Create `Domain/ValueObjects/SubcategoryName.cs` — validated 1–50 chars. Test: empty, boundary
- [x] 1.4 Create `Domain/Entities/Subcategory.cs` — AggregateRoot with UserId?, CategoryId, Name, IsAutoCreated. Test: invariants
- [x] 1.5 Modify `Domain/Entities/Transaction.cs` — add BankCategory, BankSubcategory, SubcategoryId, CategorySource; new ctor; Categorize sets source. Test: backward compat, UserOverride
- [x] 1.6 Create `Domain/Repositories/ISubcategoryRepository.cs` — 5 methods
- [x] 1.7 Create `Domain/Repositories/IBankCategoryTranslationRepository.cs` — read-only lookup
- [x] 1.8 Modify `Domain/Repositories/ICategoryRepository.cs` — remove GetSystemDefaultsAsync
- [x] 1.9 Modify `Domain/Services/CategoryService.cs` — remove GetSystemDefaults(), cache, CreateDefault, system-name check. Test: no longer blocks system names

## Phase 2: Infrastructure

- [x] 2.1 Modify `Infrastructure/Persistence/TransactionRow.cs` — add 4 columns, update ToDomain/FromDomain. Test: null→Legacy
- [x] 2.2 Create `Infrastructure/Persistence/SubcategoryRow.cs` — Postgrest DTO
- [x] 2.3 Create `Infrastructure/Persistence/SupabaseSubcategoryRepository.cs` — ISubcategoryRepository
- [x] 2.4 Create `Infrastructure/Persistence/BankCategoryTranslationRow.cs` — Postgrest DTO (read-only)
- [x] 2.5 Create `Infrastructure/Persistence/SupabaseBankCategoryTranslationRepository.cs` — read-only lookup
- [x] 2.6 Modify `Infrastructure/DependencyInjection.cs` — register new repos/services

## Phase 3: Application

- [x] 3.1 Create `Application/Services/ResolutionResult.cs` — record (CategoryId?, SubcategoryId?, CategorySource)
- [x] 3.2 Create `Application/Services/IBankCategoryResolutionService.cs` — ResolveAsync interface
- [x] 3.3 Create `Application/Services/BankCategoryResolutionService.cs` — algorithm: normalize→translate→name-match→subcat. Test: 4 scenarios
- [x] 3.4 Modify `ImportTransactionsFromPdfCommandHandler.cs` — inject resolution service, call per row, remove SeedSystemDefaults. Test: raw values saved, no seed call
- [x] 3.5 Modify `GetCategoriesQueryHandler.cs` — remove SeedSystemDefaults call, IsSystemDefault sort. Test: no seed call
- [x] 3.6 Delete `SeedSystemDefaultsCommand.cs` — entire file
- [x] 3.7 Delete `SeedSystemDefaultsCommandHandler.cs` — entire file
- [x] 3.8 Modify `TransactionDto.cs` — add 5 new fields

## Phase 4: DB Migration

- [x] 4.1 Applied migration `remove_system_default_categories` via Supabase. 0 system default rows remaining. FK integrity verified (0 transactions referenced them).

## Phase 5: Cleanup & Verify

- [x] 5.1 `dotnet test` — 356/356 passing (baseline 352 + 4 new ResolutionService/mapping tests)
- [x] 5.2 No remaining references to SeedSystemDefaults, GetSystemDefaults, CategoryService.GetSystemDefaults (dead method in SupabaseCategoryRepository.cs not in interface — harmless orphan)
