# Proposal: Fix Transaction Category Warnings

## Intent

Cerrar los dos warnings documentados en el archive report de `fix-transaction-category-retrieval`.
El comportamiento en producción ya es correcto en ambos casos; la brecha es de cobertura y eficiencia:

1. **CR-2e sin test de repositorio** — `SupabaseBankCategoryTranslationRepository` ya implementa exact-before-generic, pero ningún test automatizado lo verifica al nivel de infraestructura. Un cambio en el orden de queries no fallaría ningún test actual.
2. **N+1 en `GetTransactionsQueryHandler`** — líneas 85-97 hacen `GetByIdAsync` por cada `categoryId` distinto. Los handlers hermanos ya usan `GetByUserIdAsync(userId)` + diccionario; este handler quedó sin actualizar.

## Scope

### In Scope
- Introducir dos métodos `protected internal virtual` en `SupabaseBankCategoryTranslationRepository` como seam de testabilidad y añadir tests de comportamiento para CR-2e.
- Reemplazar el loop N+1 de categorías en `GetTransactionsQueryHandler.Handle()` (líneas 85-97) con el patrón batch `GetByUserIdAsync(userId)` + diccionario.
- Añadir tests de aplicación para `GetTransactionsQueryHandler`: category-name mapping, `GetByUserIdAsync` llamado una vez y `GetByIdAsync` nunca llamado.

### Out of Scope
- Cambios en entidades de dominio, value objects o contratos de repositorio.
- Refactorización de otros handlers o tests de infraestructura.
- Apertura de PRs (marcada como pendiente en el archive report).

## Capabilities

### New Capabilities
None

### Modified Capabilities
- `category-resolution`: añadir escenarios CR-2e-infra (test de comportamiento de repositorio para precedencia exacta) y DT-1d (test de batch de categorías en `GetTransactionsQueryHandler`).

## Approach

**Seam de infraestructura (CR-2e):** extraer `ExecuteExactMatchQueryAsync` y `ExecuteGenericMatchQueryAsync` como `protected internal virtual` en el repositorio. Una subclase `TestableSupabaseBankCategoryTranslationRepository` en el proyecto de tests las sobreescribe con datos en memoria. Escenarios: exacta-gana, fallback-genérico, null.

**N+1 fix (DT-1d):** reemplazar las líneas 85-97 con `var categories = await _categoryRepo.GetByUserIdAsync(userId); var categoryLookup = categories.ToDictionary(c => c.Id, c => c.Name.Value);` — idéntico a `GetRecentTransactionsQueryHandler`. Tests Moq: `Verify(GetByUserIdAsync, Times.Once())` y `Verify(GetByIdAsync, Times.Never())`.

## Affected Areas

| Área | Impacto | Descripción |
|------|---------|-------------|
| `src/SauronSheet.Infrastructure/Persistence/SupabaseBankCategoryTranslationRepository.cs` | Modificado | +2 métodos `protected internal virtual` como seam |
| `tests/SauronSheet.Infrastructure.Tests/Persistence/SupabaseBankCategoryTranslationRepositoryTests.cs` | Modificado | Añadir tests de comportamiento CR-2e |
| `src/SauronSheet.Application/Features/Transactions/Queries/GetTransactionsQueryHandler.cs` | Modificado | Reemplazar loop N+1 con batch `GetByUserIdAsync` |
| `tests/SauronSheet.Application.Tests/Features/Transactions/Queries/GetTransactionsQueryHandlerTests.cs` | Modificado | Añadir tests de batch de categorías y name-mapping |

## Risks

| Riesgo | Probabilidad | Mitigación |
|--------|-------------|------------|
| `Supabase.Client` requiere args no-null en el ctor base | Baja | Subclase de test pasa `null!`; los métodos seam cortocircuitan antes de usar el cliente |
| `GetByUserIdAsync` trae todas las categorías del usuario | Baja | Escala aceptable; mismo patrón que handlers hermanos ya en producción |
| Seam expone internals más de lo ideal | Baja | Scoped a `protected internal`; visible sólo en test assembly |

## Rollback Plan

Cambio confinado a 4 archivos, sin migración ni cambio de esquema. Revertible con `git revert` sobre el único PR. El handler regresa al loop previo; los tests de repositorio se eliminan.

## Dependencies

- Los commits del change `fix-transaction-category-retrieval` deben estar mergeados antes del review de este PR (warning 3 del archive report: commits en master sin PR abierto).

## Success Criteria

- [ ] `dotnet test` pasa sin fallos (baseline: 382/382 green)
- [ ] `SupabaseBankCategoryTranslationRepositoryTests` contiene al menos un test de comportamiento para CR-2e (exact-before-generic)
- [ ] `GetTransactionsQueryHandlerTests` verifica `GetByUserIdAsync` called `Times.Once()` y `GetByIdAsync` called `Times.Never()` para lookup de categorías
- [ ] `GetTransactionsQueryHandler` no contiene ninguna llamada a `GetByIdAsync` para resolución de categorías
- [ ] Sin tests marcados `[Skip]` en esta change
