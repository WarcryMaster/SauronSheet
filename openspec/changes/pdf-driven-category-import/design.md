# Design: PDF-Driven Category Import

> **Revision note (v2)**: La versión anterior asumía incorrectamente que los UNIQUE constraints existentes `(user_id, name)` y `(user_id, category_id, name)` eran suficientes para deduplicación normalizada. Estos constraints solo protegen contra duplicados EXACTOS — "Alimentación" y "alimentacion" serían tratados como categorías distintas. El spec PCE-2 exige deduplicación por clave normalizada (sin diacríticos, case-insensitive). Esta revisión añade columna `normalized_name` con UNIQUE constraint a nivel DB.

## Technical Approach

Evolucionar `BankCategoryResolutionService` de lookup-only a **get-or-add**, eliminar las listas cerradas del parser ING, y añadir una columna `normalized_name` a `categories` y `subcategories` para enforcement de deduplicación a nivel DB. La normalización la computa la aplicación (`CategoryNormalizer`) y la persiste junto con el `name` raw — el DB simplemente enforce UNIQUE sobre el valor pre-computado.

## Architecture Decisions

| # | Decision | Alternatives Considered | Rationale |
|---|----------|------------------------|-----------|
| D1 | Evolucionar `IBankCategoryResolutionService` con método `ResolveOrCreateAsync` en lugar de crear interfaz nueva | Nueva interfaz `IPdfCategoryResolverService` | El handler ya inyecta este servicio; añadir método mantiene cohesión y evita nueva inyección. La interfaz vieja (`ResolveAsync`) permanece para posible uso futuro sin creación. |
| D2 | Normalización como método estático `CategoryNormalizer` en Application | Value Object en Domain / Extension method | Es lógica de deduplicación (Application concern); Domain no la necesita. Método estático puro = testable sin dependencias. |
| D3 | Columna `normalized_name` (application-owned) + partial UNIQUE INDEX (`WHERE user_id IS NOT NULL`) | Functional index `lower(unaccent(name))` / `ADD CONSTRAINT UNIQUE` / RPC SQL | (a) `unaccent` no está instalado en Supabase y su comportamiento podría no coincidir exactamente con C# `RemoveDiacritics`; (b) columna explícita = single source of truth para normalización (solo `CategoryNormalizer`); (c) queries directas WHERE normalized_name = ? sin funciones SQL; (d) partial index excluye system defaults (`user_id IS NULL`) de forma explícita, más correcto que UNIQUE constraint global para el esquema multi-tenant. |
| D4 | Catch `PostgrestException` con code 23505 para retry-get tras insert conflict | Función SQL `get_or_create_category` | El UNIQUE constraint sobre `normalized_name` lo protege; Postgrest .NET no soporta ON CONFLICT nativamente. Insert + catch + fetch es portable y no requiere función RPC. |
| D5 | Parser ING usa posición-first heuristic: línea 1=Category, línea 2=SubCategory (tras fecha) | Mantener KnownCategories como fallback | La estructura del PDF es fija (ING layout); posición es determinista y no pierde valores desconocidos. KnownCategories eliminado completamente. |
| D6 | Display helper usa `CategorySource` para decidir qué mostrar | Solo presencia de BankCategory | CategorySource es semánticamente correcto; ya está en TransactionDto. Permite distinguir UserOverride de AutoMatched sin ambigüedad. |

## Data Flow

```
PDF Stream
    │
    ▼
IngBankPdfParser (Infrastructure)
    │  extrae literales: Category, SubCategory (sin filtro)
    ▼
RawTransactionRow (Domain VO)
    │
    ▼
ImportTransactionsFromPdfCommandHandler (Application)
    │  por cada row:
    │  ├─ valida campos obligatorios
    │  ├─ detecta duplicados
    │  └─ llama ResolveOrCreateAsync(userId, rawCat, rawSubcat)
    ▼
BankCategoryResolutionService.ResolveOrCreateAsync (Application)
    │  1. normaliza → CategoryNormalizer.Normalize(rawCat) → normalizedKey
    │  2. busca en DB: WHERE user_id = ? AND normalized_name = normalizedKey
    │  3. si existe → reutiliza (source=AutoMatched)
    │  4. si no existe → insert (name=raw, normalized_name=normalizedKey)
    │     → si 23505 conflict → retry-get por normalized_name
    │  5. busca/crea Subcategory (misma lógica normalized_name)
    │  6. retorna ResolutionResult(categoryId, subcategoryId, AutoMatched)
    ▼
Transaction persisted con BankCategory + CategoryId + SubcategoryId + Source
    │
    ▼
TransactionCategoryDisplayHelper (Frontend)
    │  UserOverride → CategoryName
    │  AutoMatched/RawOnly + BankCategory ≠ null → BankCategory
    │  Legacy/sin BankCategory → CategoryName
```

## File Changes

| File | Action | Description |
|------|--------|-------------|
| `src/SauronSheet.Infrastructure/Persistence/Migrations/011_AddNormalizedNameColumns.sql` | Create | Migración: columna `normalized_name`, backfill, drop old UNIQUE, crear `CREATE UNIQUE INDEX ... WHERE user_id IS NOT NULL` (partial index, not ADD CONSTRAINT UNIQUE) |
| `src/SauronSheet.Application/Services/CategoryNormalizer.cs` | Create | Clase estática: `Normalize(string?) → string?` — lowercase, remove diacritics, trim |
| `src/SauronSheet.Application/Services/IBankCategoryResolutionService.cs` | Modify | Añadir `ResolveOrCreateAsync(UserId, string?, string?, CancellationToken)` |
| `src/SauronSheet.Application/Services/BankCategoryResolutionService.cs` | Modify | Implementar `ResolveOrCreateAsync` con get-or-add via `normalized_name` + conflict handling |
| `src/SauronSheet.Infrastructure/PDF/Parsers/IngBankPdfParser.cs` | Modify | Eliminar `KnownCategories`/`KnownSubCategories`; usar posición para extraer literales |
| `src/SauronSheet.Application/Features/Transactions/Commands/ImportTransactionsFromPdfCommandHandler.cs` | Modify | Cambiar llamada de `ResolveAsync` a `ResolveOrCreateAsync` |
| `src/SauronSheet.Frontend/Helpers/TransactionCategoryDisplayHelper.cs` | Modify | Lógica basada en `CategorySource` del DTO |
| `src/SauronSheet.Domain/Repositories/ICategoryRepository.cs` | Modify | Añadir `FindByNormalizedNameAndUserAsync(UserId, string normalizedName)` |
| `src/SauronSheet.Domain/Repositories/ISubcategoryRepository.cs` | Modify | Añadir `FindByNormalizedNameAsync(UserId, CategoryId, string normalizedName)` |
| `src/SauronSheet.Infrastructure/Persistence/CategoryRow.cs` (inline in SupabaseCategoryRepository.cs) | Modify | Añadir propiedad `NormalizedName`; mapear en `FromDomainForInsert` |
| `src/SauronSheet.Infrastructure/Persistence/SubcategoryRow.cs` | Modify | Añadir propiedad `NormalizedName` |
| `src/SauronSheet.Infrastructure/Persistence/SupabaseCategoryRepository.cs` | Modify | Implementar `FindByNormalizedNameAndUserAsync` con filtro Postgrest en `normalized_name` |
| `src/SauronSheet.Infrastructure/Persistence/SupabaseSubcategoryRepository.cs` | Modify | Implementar `FindByNormalizedNameAsync`; set `NormalizedName` on insert |
| `tests/SauronSheet.Application.Tests/Services/CategoryNormalizerTests.cs` | Create | Tests para normalización: diacríticos, casing, whitespace |
| `tests/SauronSheet.Application.Tests/Services/BankCategoryResolutionServiceTests.cs` | Modify | Tests para flujo get-or-add, concurrencia, system default exclusion, normalized dedup |
| `tests/SauronSheet.Frontend.Tests/Helpers/TransactionCategoryDisplayHelperTests.cs` | Create/Modify | Tests para los 4 escenarios de display |
| `tests/SauronSheet.Infrastructure.Tests/PDF/Parsers/IngBankPdfParserSingleLineTests.cs` | Create | Tests para PCE-1a single-line guard: `ParseTextColumns` retorna `(null, null, description, null)` — categoría/subcategoría siempre null en path single-line (3 métodos / 5 casos) |

## Interfaces / Contracts

```csharp
// Application/Services/CategoryNormalizer.cs
public static class CategoryNormalizer
{
    /// <summary>
    /// Deterministic normalization for deduplication:
    /// lowercase → remove diacritics (via String.Normalize FormD + strip combining chars) → trim → null if empty.
    /// MUST be the SINGLE source of truth — DB stores the output, never computes it.
    /// </summary>
    public static string? Normalize(string? value);
}

// Application/Services/IBankCategoryResolutionService.cs (new method)
public interface IBankCategoryResolutionService
{
    // Existing — lookup only, no creation
    Task<ResolutionResult> ResolveAsync(UserId userId, string? bankCategory, string? bankSubcategory, CancellationToken ct);

    // NEW — get-or-add: finds by normalized_name column or creates with both name + normalized_name
    Task<ResolutionResult> ResolveOrCreateAsync(UserId userId, string? rawCategory, string? rawSubcategory, CancellationToken ct);
}

// Domain/Repositories/ICategoryRepository.cs (new method)
public interface ICategoryRepository
{
    // ... existing methods ...
    
    /// <summary>
    /// Find a user category by its pre-computed normalized name.
    /// Used by get-or-add flow — queries the normalized_name column directly.
    /// </summary>
    Task<Category?> FindByNormalizedNameAndUserAsync(UserId userId, string normalizedName);
}

// Domain/Repositories/ISubcategoryRepository.cs (new method)
public interface ISubcategoryRepository
{
    // ... existing methods ...
    
    /// <summary>
    /// Find subcategory by pre-computed normalized name within a category.
    /// </summary>
    Task<Subcategory?> FindByNormalizedNameAsync(UserId userId, CategoryId categoryId, string normalizedName);
}
```

### Postgrest DTO Changes

```csharp
// CategoryRow — add column mapping
[Column("normalized_name")]
public string NormalizedName { get; set; } = "";

// SubcategoryRow — add column mapping
[Column("normalized_name")]
public string NormalizedName { get; set; } = "";
```

### Get-or-Add Algorithm (BankCategoryResolutionService.ResolveOrCreateAsync)

```csharp
// Pseudocode:
var normalizedKey = CategoryNormalizer.Normalize(rawCategory);
if (normalizedKey == null) return ResolutionResult(null, null, RawOnly);

// 1. Lookup by normalized_name (DB query, not in-memory)
var existing = await _categoryRepo.FindByNormalizedNameAndUserAsync(userId, normalizedKey);
if (existing != null && !existing.IsSystemDefault)
    // found → use it
    
// 2. Skip system defaults explicitly
// 3. Create: name = rawCategory (literal from PDF), normalized_name = normalizedKey
try { await _categoryRepo.AddAsync(newCategory); }
catch (PostgrestException ex) when (ex.StatusCode == 409 || /* 23505 */)
{
    // Conflict on UNIQUE(user_id, normalized_name) → concurrent insert won
    existing = await _categoryRepo.FindByNormalizedNameAndUserAsync(userId, normalizedKey);
}
// 4. Same pattern for subcategory
```

## Testing Strategy

| Layer | What to Test | Approach |
|-------|-------------|----------|
| Unit (Application) | `CategoryNormalizer` — diacritics, casing, null, empty, combined, idempotent | xUnit parametrized theory |
| Unit (Application) | `BankCategoryResolutionService.ResolveOrCreateAsync` — match existing by normalized, create new, skip system default, null input, 23505 conflict retry | xUnit + Moq (mock repos) |
| Unit (Frontend) | `TransactionCategoryDisplayHelper.Build` — 4 source scenarios | xUnit parametrized |
| Integration | `IngBankPdfParser.ParseAsync` — PDF with unknown categories returns raw literals | xUnit + sample PDF bytes |
| Integration | `ImportTransactionsFromPdfCommandHandler` — end-to-end mock flow verifica get-or-add called | xUnit + Moq |
| Migration | Backfill script correctness (existing names → expected normalized values) | Manual SQL verification post-deploy |

## Migration / Rollout

### SQL Migration: `011_AddNormalizedNameColumns.sql`

```sql
-- 5. Create new partial UNIQUE INDEXes on normalized_name
--    WHERE user_id IS NOT NULL: consistent with existing DB design.
--    System defaults have NULL user_id; partial index makes the intent explicit.
--    NOTE: Implemented as CREATE UNIQUE INDEX ... WHERE user_id IS NOT NULL
--    instead of ADD CONSTRAINT UNIQUE — functionally equivalent and more
--    precise for the multi-tenant schema (system defaults with NULL user_id
--    are intentionally excluded from uniqueness enforcement).
CREATE UNIQUE INDEX IF NOT EXISTS uq_categories_user_normalized_name
    ON public.categories(user_id, normalized_name)
    WHERE user_id IS NOT NULL;

CREATE UNIQUE INDEX IF NOT EXISTS uq_subcategories_user_category_normalized_name
    ON public.subcategories(user_id, category_id, normalized_name)
    WHERE user_id IS NOT NULL;

-- 6. Index for lookup performance
CREATE INDEX IF NOT EXISTS idx_categories_user_normalized 
  ON public.categories(user_id, normalized_name);
CREATE INDEX IF NOT EXISTS idx_subcategories_user_cat_normalized 
  ON public.subcategories(user_id, category_id, normalized_name);
```

### Rollout Plan

1. **Migration first** — deploy `011_AddNormalizedNameColumns.sql` (backfills existing data; non-breaking)
2. **Code deploy** — parser + service + repos + display helper
3. Existing rows use `lower(trim(name))` as initial normalized value; application will correctly match them since `CategoryNormalizer.Normalize` also lowercases and trims (diacritics removal only adds precision for new imports)
4. **Backfill refinement** (optional, post-deploy): run one-time app script to recompute `normalized_name` for all existing categories using the exact C# normalizer for full diacritics parity
5. **Rollback**: revert code + revert migration (re-add old constraints, drop column) — safe since only 4 categories exist currently

### Supabase/Postgrest Constraints

- **Insert**: Application MUST set both `name` (raw literal) and `normalized_name` (pre-computed) on every insert via `CategoryRow.FromDomainForInsert()`
- **Conflict detection**: Postgrest .NET client throws `PostgrestException` on UNIQUE violation (HTTP 409 or Postgres error 23505)
- **RLS**: existing policies unaffected — new column is data, not access-control
- **Postgrest gotcha**: `WHERE normalized_name = ?` works directly since it's a string equality filter (no method calls in lambda — critical per AGENTS.md pitfall)

## Risks

| Risk | Likelihood | Mitigation |
|------|------------|------------|
| Backfill `lower(trim(name))` no cubre diacríticos para datos existentes | Low (4 categorías actuales, todas ASCII-safe) | Post-deploy script de re-normalización; verificar manualmente las 4 rows existentes |
| C# normalizer drift vs backfill initial values | Low | Test de integración que compara normalizer output vs DB stored value; single source of truth en `CategoryNormalizer` |
| Concurrent import race en get-or-add | Med | UNIQUE constraint en `normalized_name` + catch 23505 + retry-get (DB enforced, no window) |
| Drop de old UNIQUE `(user_id, name)` permite dos categorías con mismo raw name pero diferente normalización | Very Low | En la práctica, `CategoryNormalizer` es determinista — mismo input siempre produce mismo output; dos raw names que normalizan igual son el mismo concepto |

## Open Questions

- Ninguna. La estrategia `normalized_name` column resuelve el gap de deduplicación con enforcement a nivel DB.
