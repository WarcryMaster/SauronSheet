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

- [x] 3.1 `ImportTransactionsFromPdfCommandHandler`: swap `ResolveAsync` → `ResolveOrCreateAsync`; remove unused `_categoryRepo`
- [x] 3.2 RED+GREEN: handler test IH-1 — new PDF category → category created + AutoMatched transaction
- [x] 3.3 RED: DisplayHelper tests DH-1a/1b/1c (UserOverride→CategoryName)/1d (legacy null)
- [x] 3.4 GREEN: `TransactionCategoryDisplayHelper.Build()` — `UserOverride→CategoryName`; `BankCat!=null→BankCat`; else `CategoryName`; else Uncategorized
- [x] 3.5 REFACTOR: run `IndexModelTests`; no regression
- [x] 3.6 DI audit: resolver wired; no stale injections
- [x] 3.7 `dotnet build` + `dotnet test` (5 projects) — coverage ≥80% Domain, ≥70% Application

## Phase 4: Post-Verify Remediation [PR4 — hotfix slice]

> **Origen**: verify report PASS WITH WARNINGS (2026-05-25). Tres advertencias pendientes.
> Sin cambios funcionales; solo test faltante + correcciones de artefactos.

### Remediation Workload Forecast

| Field | Value |
|-------|-------|
| Estimated changed lines | ~30–50 (1 clase de test ≈30–40 líneas; resto son artefactos) |
| 400-line budget risk | Low |
| Chained PRs recommended | No |
| Suggested split | Single PR4 hotfix |
| Delivery strategy | auto-chain |
| Chain strategy | feature-branch-chain (PR4 base = PR3 branch) |

Decision needed before apply: No
Chained PRs recommended: No
Chain strategy: feature-branch-chain
400-line budget risk: Low

- [x] 4.1 Actualizar artefacto apply-progress (Engram): expandir tabla TDD con columnas TRIANGULATE y SAFETY NET; mapear evidencia existente a las 5 columnas por cada fila de tarea.
- [x] 4.2 Actualizar artefacto design (Engram): sustituir texto "ADD CONSTRAINT UNIQUE" por "partial UNIQUE INDEX (WHERE user_id IS NOT NULL)" para reflejar implementación real de migration 011.
- [x] 4.3 RED: `IngBankPdfParserSingleLineTests` — assert que `ParseTextColumns` en path single-line retorna `(null, null, description, null)`; categoría y subcategoría siempre null (PCE-1a single-line guard).
- [x] 4.4 GREEN: confirmar test pasa sin cambios de producción (comportamiento ya existe; solo faltaba el test).
- [x] 4.5 `dotnet test` — ≥456 tests green (455 previos + ≥1 nuevo); sin regresión.
- [ ] 4.6 Re-ejecutar sdd-verify → confirmar PASS sin advertencias.
