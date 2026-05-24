# Design: Category Selector Inline Create

## Technical Approach

Reemplazar el `<select>` de categorías en Add Transaction por un `<input list>` + `<datalist>` siguiendo el patrón ImportedFrom ya implementado en `/transactions`. La resolución nombre→ID ocurre en el PageModel (no en el command handler), manteniendo `CreateTransactionCommand` inalterado.

## Architecture Decisions

### Decision: Datalist sobre Select

| Opción | Tradeoff | Decisión |
|--------|----------|----------|
| `<select>` | No es searchable nativo, UX pobre con muchas categorías | ❌ Descartado |
| `<input list>` + `<datalist>` | Searchable nativo HTML5, mismo patrón que ImportedFrom, 0 JS | ✅ Elegido |
| Select2 / Choices.js | Dependencia JS adicional, sobreingeniería para este caso | ❌ Descartado |
| Autocomplete con fetch | Requiere endpoint API, sobrecarga de red | ❌ Descartado |

### Decision: Resolución en PageModel vs Command Handler

| Opción | Tradeoff | Decisión |
|--------|----------|----------|
| Resolver en PageModel | 0 cambios en Application Layer, lógica cohesiva con la vista | ✅ Elegido |
| Modificar CreateTransactionCommand | Aceptar string y resolver internamente — mezcla concerns de UI con dominio | ❌ Descartado |
| Nuevo middleware/resolver | Overengineering para una sola página | ❌ Descartado |

### Decision: CategoryType en inline create

| Opción | Tradeoff | Decisión |
|--------|----------|----------|
| Usar `CategoryType.Expense` por defecto | Simple, pero incorrecto para ingresos | ❌ Descartado |
| Usar el tipo de Amount (signo) | Amount ya tiene signo en el form — positivo=Income, negativo=Expense | ✅ Elegido |

## Data Flow

```
POST /transactions/add
  │
  ├─ Input.CategoryName = null/""     → CategoryId = null
  ├─ Input.CategoryName match (CI)    → CategoryId = matched.Id
  └─ Input.CategoryName sin match     → CreateCategoryCommand(name, type)
                                            │
                                            └─ CategoryId (Guid)
  │
  └─ CreateTransactionCommand(..., CategoryId)
```

```
GET /transactions/add
  │
  └─ GetCategoriesQuery
       │
       └─ List<CategoryDto> → nombres para el datalist
```

## File Changes

| File | Action | Description |
|------|--------|-------------|
| `Frontend/Pages/Transactions/Add.cshtml` | Modify | `<select>` → `<input list="categories">`+`<datalist>`, remove IsSystemDefault icon |
| `Frontend/Pages/Transactions/Add.cshtml.cs` | Modify | InputModel: `CategoryId`→`CategoryName`. Add `CategoryName` property, resolver logic in OnPostAsync, derive CategoryType from Amount sign |
| `Categories/DTOs/CategoryDto.cs` | Modify | Remove `IsSystemDefault` property |
| `Categories/Queries/GetCategoriesQueryHandler.cs` | Modify | Remove `c.IsSystemDefault` reference in Select |

## Interfaces / Contracts

No new interfaces. The PageModel internally resolves name→ID:

```csharp
// In Add.cshtml.cs — new resolution logic
private async Task<Guid?> ResolveCategoryId(string? categoryName)
{
    if (string.IsNullOrWhiteSpace(categoryName))
        return null;

    var match = Categories.FirstOrDefault(c =>
        c.Name.Equals(categoryName, StringComparison.OrdinalIgnoreCase));

    if (match is not null)
        return match.Id;

    var type = Input.Amount >= 0 ? CategoryType.Income : CategoryType.Expense;
    return await _mediator.Send(new CreateCategoryCommand(categoryName, type));
}
```

## Testing Strategy

| Layer | What to Test | Approach |
|-------|-------------|----------|
| Unit | PageModel resolution logic | Mock IMediator, verify CategoryId output for each scenario (match/new/null) |
| Integration | Full POST flow | Integration test: PageModel → command resolution |

## Migration / Rollout

No migration required. No feature flag. Ship as single deploy.

## Open Questions

None.
