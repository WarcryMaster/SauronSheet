# Tasks: PDF-Driven Category Import

## Review Workload Forecast

| Field | Value |
|-------|-------|
| Estimated changed lines | ~950 (PR1 ≈400 · PR2 ≈380 · PR3 ≈170) |
| 400-line budget risk | High |
| Chained PRs recommended | Yes |
| Suggested split | PR1 → PR2 → PR3 |
| Delivery strategy | auto-chain |
| Chain strategy | feature-branch-chain |

Decision needed before apply: No
Chained PRs recommended: Yes
Chain strategy: feature-branch-chain
400-line budget risk: High

### Suggested Work Units

| Unit | Goal | PR | Notes |
|------|------|----|-------|
| 1 — Foundation | Migration 011 + CategoryNormalizer + Domain + Infra | PR1 | Base = tracker `feature/pdf-driven-category-import` |
| 2 — Service + Parser | get-or-add + IngBankPdfParser cleanup | PR2 | Base = PR1 branch |
| 3 — Wiring + Display | Handler + DisplayHelper + SystemDefault guard | PR3 | Base = PR2 branch |

## Phase 1: Migration + Foundation [PR1]

**⚠️ RISK backfill drift** — SQL uses `lower(trim())`; C# adds diacritics. Task 1.5 blocks PR1 merge on divergence.

- [x] 1.1 RED: `CategoryNormalizerTests` — PCE-2a (diacritics), PCE-2b (casing), PCE-2c (combined)
- [x] 1.2 GREEN: `Application/Services/CategoryNormalizer.cs` — `Normalize(string?) → string?`
- [x] 1.3 REFACTOR: null/whitespace edge cases; single entry point only
- [x] 1.4 Create `Migrations/011_AddNormalizedNameColumns.sql`: add column, backfill `lower(trim(name))`, SET NOT NULL, drop old UNIQUEs, new `UNIQUE(user_id, normalized_name)`, indexes
- [x] 1.5 Integration test: C# normalizer output == SQL backfill per existing row (drift gate)
- [x] 1.6 Add `NormalizedName` to `CategoryRow` + `SubcategoryRow`
- [x] 1.7 Add `FindByNormalizedNameAndUserAsync` to `ICategoryRepository`; `FindByNormalizedNameAsync` to `ISubcategoryRepository`
- [x] 1.8 RED+GREEN: implement both find methods in Supabase repos; set `normalized_name` on insert
- [x] 1.9 Apply migration 011; confirm UNIQUE constraint active

## Phase 2: Service + Parser [PR2]

**⚠️ RISK system default** — 2.4 RED test PCE-3c BEFORE implementation.
**⚠️ RISK parser regression** — 2.6 golden test BEFORE removing KnownCategories.

- [x] 2.1 Add `ResolveOrCreateAsync` to `IBankCategoryResolutionService`
- [x] 2.2 RED: PCE-3a (exists→reuse), PCE-3b (missing→create), PCE-3d (null→RawOnly), PCE-3e (23505→retry-get)
- [x] 2.3 GREEN: `BankCategoryResolutionService.ResolveOrCreateAsync` — normalized lookup → INSERT → catch 23505 → retry-get
- [x] 2.4 RED: PCE-3c — `IsSystemDefault=true` bypassed; new user category created
- [x] 2.5 GREEN: `IsSystemDefault` guard in resolver
- [x] 2.6 RED: golden regression — former KnownCategories row returns literal (PCE-1a)
- [x] 2.7 GREEN: remove `KnownCategories`/`KnownSubCategories` from `IngBankPdfParser`; position-first extraction in both parse methods
- [x] 2.8 RED+GREEN: subcategory get-or-add PCE-4a/4b/4c/4d, scoped `(userId, categoryId, normalizedKey)`, `IsAutoCreated=true`
- [x] 2.9 REFACTOR: `ResolveAsync` tests + `AmountNormalizationTests` still green

## Phase 3: Handler + Display [PR3]

**⚠️ RISK UserOverride regression** — 3.3 RED for DH-1c BEFORE touching DisplayHelper; 3.5 runs `IndexModelTests`.
**⚠️ RISK import path isolation** — `ICategoryResolutionService` (manual path) must not be modified.

- [ ] 3.1 `ImportTransactionsFromPdfCommandHandler`: swap `ResolveAsync` → `ResolveOrCreateAsync`; remove unused `_categoryRepo`
- [ ] 3.2 RED+GREEN: handler test IH-1 — new PDF category → category created + AutoMatched transaction
- [ ] 3.3 RED: DisplayHelper tests DH-1a/1b/1c (UserOverride→CategoryName)/1d (legacy null)
- [ ] 3.4 GREEN: `TransactionCategoryDisplayHelper.Build()` — `UserOverride→CategoryName`; `BankCat!=null→BankCat`; else `CategoryName`; else Uncategorized
- [ ] 3.5 REFACTOR: run `IndexModelTests`; no regression
- [ ] 3.6 DI audit: resolver wired; no stale injections
- [ ] 3.7 `dotnet build` + `dotnet test` (5 projects) — coverage ≥80% Domain, ≥70% Application
