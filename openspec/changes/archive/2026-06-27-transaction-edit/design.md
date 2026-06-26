# Design: Transaction Edit

## Technical Approach

Implementar edición completa de transacciones siguiendo el patrón existente de `Budgets/Edit`. Se añade un método `Update()` al agregado `Transaction`, un query `GetTransactionByIdQuery` para cargar datos, un command `UpdateTransactionCommand` para persistir cambios, y una página Razor `Transactions/Edit.cshtml` con formulario edit + resumen read-only. La detección de duplicados y validación de tenant se manejan en el handler, no en la UI.

## Architecture Decisions

| Decisión | Opciones | Tradeoff | Decisión |
|----------|----------|----------|----------|
| Método domain para editar | `Update()` general vs setters individuales | General: 1 call, 1 `UpdatedAt`. Individual: más granular pero más surface area | `Update()` con todos los campos editables. Preserva `ImportedFrom`, `BankCategory`, `BankSubcategory`, `Balance` (metadatos de importación) |
| Query vs PageModel directo | `GetTransactionByIdQuery` + MediatR vs PageModel llama repo directo | Query: consistente con CQRS, testable. Directo: menos código | Query + MediatR. Mantiene Clean Architecture (Frontend → Application → Domain) |
| Duplicado: excluir self | Pasar `transactionId` al `ExistsDuplicateAsync` vs filtrar en handler | El repo no soporta exclude-id. Filtrar en handler: 2 queries | Excluir en memoria: si el único duplicado es la propia transacción (mismos date/amount/description/balance), no es duplicado real |
| CategorySource al editar | Siempre `UserOverride` vs preservar si no cambia categoría | Si el usuario edita sin tocar categoría, forzar `UserOverride` pierde info | Si `categoryId` cambia → `UserOverride`. Si no cambia → preservar `CategorySource` existente |
| Subcategory validation | Validar en handler vs en domain | Domain no conoce repositorios. Handler ya valida categoría | Handler valida: subcategory debe existir y pertenecer a la categoría seleccionada |

## Data Flow

```
Browser ──GET──→ Edit.cshtml.cs ──→ GetTransactionByIdQuery ──→ ITransactionRepository.GetByIdAsync
         │                                    │
         │                              ICategoryRepository (batch names)
         │                              ISubcategoryRepository (batch names)
         │                                    │
         └──── HTML form ←──────────── TransactionDto ─┘

Browser ──POST─→ Edit.cshtml.cs ──→ UpdateTransactionCommand ──→ Handler:
         │                              1. GetByIdAsync + tenant check
         │                              2. Validate category ownership
         │                              3. Validate subcategory ∈ category
         │                              4. ExistsDuplicateAsync (5-arg, exclude self)
         │                              5. transaction.Update(...)
         │                              6. UpdateAsync
         └── Redirect /transactions ←── TempData["SuccessMessage"]
```

## File Changes

| File | Action | Descripción |
|------|--------|-------------|
| `src/SauronSheet.Domain/Entities/Transaction.cs` | Modify | Añadir `Update(Money, DateTime, string, CategoryId?, SubcategoryId?, CategorySource)` |
| `src/SauronSheet.Application/Features/Transactions/Queries/GetTransactionByIdQuery.cs` | Create | Query record + Handler: load by id, tenant check, enrich with category/subcategory names |
| `src/SauronSheet.Application/Features/Transactions/Commands/UpdateTransactionCommand.cs` | Create | Command record con todos los campos editables |
| `src/SauronSheet.Application/Features/Transactions/Commands/UpdateTransactionCommandHandler.cs` | Create | Handler: tenant, category/subcategory validation, duplicate check, `Update()`, `UpdateAsync` |
| `src/SauronSheet.Frontend/Pages/Transactions/Edit.cshtml.cs` | Create | PageModel: `[Authorize]`, OnGet carga DTO, OnPost dispatch command |
| `src/SauronSheet.Frontend/Pages/Transactions/Edit.cshtml` | Create | Resumen read-only + formulario edit (Flatpickr, Alpine.js, category datalist) |
| `src/SauronSheet.Frontend/Pages/Transactions/Index.cshtml` | Modify | Añadir botón "Edit" (✏️) en columna Actions |
| `tests/.../UpdateTransactionCommandHandlerTests.cs` | Create | 5 escenarios: happy path, not found, wrong user, duplicate, invalid subcategory |

## Interfaces / Contracts

```csharp
// Domain: Transaction.Update()
public void Update(
    Money amount,
    DateTime date,
    string description,
    CategoryId? categoryId,
    SubcategoryId? subcategoryId,
    CategorySource categorySource)
{
    if (amount == null) throw new ArgumentNullException(nameof(amount));
    if (string.IsNullOrWhiteSpace(description))
        throw new ArgumentException("Description is required.", nameof(description));

    Amount = amount;
    Date = date;
    Description = description;
    CategoryId = categoryId;
    SubcategoryId = subcategoryId;
    CategorySource = categorySource;
    UpdatedAt = DateTime.UtcNow;
}
```

```csharp
// Application: Query
public record GetTransactionByIdQuery(Guid TransactionId) : IRequest<TransactionDto>;

// Application: Command
public record UpdateTransactionCommand(
    Guid TransactionId,
    DateTime Date,
    string Description,
    decimal Amount,
    string Currency,
    Guid? CategoryId,
    Guid? SubcategoryId) : IRequest<Unit>;
```

## Testing Strategy

| Layer | Qué testear | Enfoque |
|-------|-------------|---------|
| Unit (Domain) | `Transaction.Update()` validaciones | Descripcion vacía → ArgumentException. Amount null → ArgumentNullException. UpdatedAt se actualiza. Import metadata no cambia |
| Unit (Application) | `UpdateTransactionCommandHandler` | 5 escenarios: happy path, not found, wrong tenant, duplicate (exclude self), subcategory fuera de categoría |
| Unit (Application) | `GetTransactionByIdQueryHandler` | Found + enriched, not found → EntityNotFoundException, wrong user → EntityNotFoundException |
| E2E | Flujo completo | Navegar a edit, modificar descripción, guardar → redirect + success. Duplicado → error visible. ID inexistente → redirect |

## Migration / Rollout

No migration required. No hay cambios de esquema de base de datos — todos los campos ya existen en la tabla `transactions`.

## Implementation Order

1. **Domain**: `Transaction.Update()` + tests (RED → GREEN)
2. **Application**: `GetTransactionByIdQuery` + handler + tests
3. **Application**: `UpdateTransactionCommand` + handler + tests
4. **Frontend**: `Edit.cshtml.cs` + `Edit.cshtml`
5. **Frontend**: Edit button en `Index.cshtml`
6. **E2E**: Tests de flujo completo

## Open Questions

- [ ] None — todos los patrones de referencia están verificados en el codebase.
