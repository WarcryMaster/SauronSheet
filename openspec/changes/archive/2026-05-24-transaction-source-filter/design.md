# Design: Transaction Source Filter

## Technical Approach

Add a searchable combobox (`<input list>` + `<datalist>`) to the /transactions filter bar that lets users filter by `ImportedFrom` (the PDF filename origin). The available source values are fetched **independently** via a new `GetDistinctImportedSourcesQuery` — not embedded in the paginated query response — keeping SRP clean. The source spec composition follows the exact pattern of the existing `CategoryId` filter.

```
GetDistinctImportedSourcesQuery ──→ List<string>  ──→ datalist options
GetTransactionsQuery(ImportedFrom) ──→ PaginatedResultDto  ──→ table rows
```

## Architecture Decisions

| Opción | Alternativas | Tradeoffs | Decisión |
|--------|-------------|-----------|----------|
| **Query separada** para fuentes vs. modificar GetTransactionsQuery | Modificar el existing query para retornar un wrapper `GetTransactionsResult` que incluya AvailableSources | Query separada = SRP limpio, sin acoplar la consulta paginada a la metadata del filtro. Coste: una round-trip extra a la DB por página (aceptable — datos locales, < 5ms). | **Query separada** `GetDistinctImportedSourcesQuery` |
| **In-memory distinct** vs. SELECT DISTINCT en repositorio | Añadir `GetDistinctImportedSourcesAsync` a `ITransactionRepository` e implementarlo con Postgrest `select()` | SELECT DISTINCT no está soportado de forma fiable por supabase-csharp/Postgrest. In-memory sobre `FindBySpecificationAsync` ya existente es más simple y evita acoplar el repositorio con filtros específicos. | **In-memory** — reusa `TransactionByUserSpecification` + LINQ `Distinct()` |
| **HTML5 `<datalist>`** vs. Select2/Choices JS | Select2: más features (ajax, agrupación). Datalist: nativo, 0 dependencias, tipado filtra opciones automáticamente | Select2 requiere añadir paquete JS/CSS, init script, y rompe MDBootstrap `form-control-sm`. Datalist funciona con cualquier clase Bootstrap. | **`<datalist>` HTML5** — suficiente para ~100 opciones, consistente con diseño actual |
| **Case-insensitive** spec | OrdinalIgnoreCase vs. InvariantCultureIgnoreCase vs. default (ordinal) | `StringComparison.OrdinalIgnoreCase` es la convención .NET para comparaciones culturalmente neutras. Postgrest por defecto usa case-sensitive, pero la spec se evalúa in-memory tras el fetch. | `OrdinalIgnoreCase` — consistente con el patrón de `CategoryService` |

## Component Overview

| Fichero | Acción | Descripción |
|---------|--------|-------------|
| `Domain/Specifications/TransactionByImportedFromSpecification.cs` | Create | Spec que matchea `t.ImportedFrom == value` (case-insensitive) |
| `Application/Features/Transactions/Queries/GetTransactionsQuery.cs` | Modify | Añadir `string? ImportedFrom = null` al record |
| `Application/Features/Transactions/Queries/GetTransactionsQueryHandler.cs` | Modify | Componer spec condicional (mismo patrón que CategoryId) |
| `Application/Features/Transactions/Queries/GetDistinctImportedSourcesQuery.cs` | Create | Query record vacío → `List<string>` |
| `Application/Features/Transactions/Queries/GetDistinctImportedSourcesQueryHandler.cs` | Create | Handler: fetch vía `TransactionByUserSpecification`, distinct en memoria, orden alfabético |
| `Frontend/Pages/Transactions/Index.cshtml.cs` | Modify | Añadir `ImportedFrom` bind + `AvailableSources` + cargar fuentes en OnGetAsync |
| `Frontend/Pages/Transactions/Index.cshtml` | Modify | Añadir `<input list="importedFromList">` + `<datalist>` en el formulario de filtros |

## Detailed Design

### 1. TransactionByImportedFromSpecification (Create)

```csharp
// Sigue el patrón exacto de TransactionByCategorySpecification
public class TransactionByImportedFromSpecification : BaseSpecification<Transaction>
{
    public TransactionByImportedFromSpecification(string importedFrom)
        : base(t => t.ImportedFrom != null
                    && t.ImportedFrom.Equals(importedFrom, StringComparison.OrdinalIgnoreCase))
    {
    }
}
```

- `string` directamente, no value object — `ImportedFrom` es un raw string en la entidad.
- Guard contra null en la expresión (no se puede llamar `.Equals()` en null).
- `OrdinalIgnoreCase` — el usuario puede escribir "nomina.pdf" y matchear "NOMINA.pdf".

### 2. GetTransactionsQuery (Modify)

```csharp
public record GetTransactionsQuery(
    int PageNumber = 1,
    int PageSize = 50,
    Guid? CategoryId = null,
    DateTime? StartDate = null,
    DateTime? EndDate = null,
    string? ImportedFrom = null) : IRequest<PaginatedResultDto<TransactionDto>>;
```

- Un único parámetro opcional al final del record.
- Default `null` = no aplicar filtro de origen.

### 3. GetTransactionsQueryHandler (Modify)

Insertar después del bloque `request.EndDate.HasValue`:

```csharp
if (!string.IsNullOrEmpty(request.ImportedFrom))
{
    var sourceSpec = new TransactionByImportedFromSpecification(request.ImportedFrom);
    spec = CompositeSpecification<Transaction>.And(spec, sourceSpec);
}
```

- Mismo patrón compositivo que `CategoryId` y `StartDate`/`EndDate`.
- `!string.IsNullOrEmpty` evita crear spec con valor vacío (no matchearía nada).

### 4. GetDistinctImportedSourcesQuery (Create)

```csharp
public record GetDistinctImportedSourcesQuery()
    : IRequest<List<string>>;
```

- Query vacía (no parámetros) — siempre devuelve fuentes del usuario logueado.

### 5. GetDistinctImportedSourcesQueryHandler (Create)

```csharp
public class GetDistinctImportedSourcesQueryHandler
    : IRequestHandler<GetDistinctImportedSourcesQuery, List<string>>
{
    private readonly ITransactionRepository _transactionRepo;
    private readonly IUserContext _userContext;

    public GetDistinctImportedSourcesQueryHandler(
        ITransactionRepository transactionRepo,
        IUserContext userContext)
    {
        _transactionRepo = transactionRepo;
        _userContext = userContext;
    }

    public async Task<List<string>> Handle(
        GetDistinctImportedSourcesQuery request,
        CancellationToken cancellationToken)
    {
        var userId = new UserId(_userContext.UserId);
        var spec = new TransactionByUserSpecification(userId);
        var transactions = await _transactionRepo.FindBySpecificationAsync(spec);

        return transactions
            .Select(t => t.ImportedFrom)
            .Where(s => !string.IsNullOrEmpty(s))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(s => s)
            .ToList();
    }
}
```

- Usa `StringComparer.OrdinalIgnoreCase` en `Distinct()` para consistencia.
- Filtra null/vacío antes del distinct.
- Orden alfabético ascendente.

### 6. Index.cshtml.cs (Modify)

```csharp
// Nuevas propiedades
[BindProperty(SupportsGet = true)]
public string? ImportedFrom { get; set; }

public List<string> AvailableSources { get; set; } = new();

// En OnGetAsync:
public async Task OnGetAsync()
{
    AvailableSources = await _mediator.Send(new GetDistinctImportedSourcesQuery());

    Transactions = await _mediator.Send(
        new GetTransactionsQuery(PageNumber, PageSize, CategoryId, StartDate, EndDate, ImportedFrom));
}
```

- `AvailableSources` se carga SIEMPRE, sin cache — los datos son locales, y así refleja cambios inmediatos tras una importación.
- `ImportedFrom` se pasa directamente al query.

### 7. Index.cshtml (Modify)

Insertar entre el date range filter y el botón Apply:

```html
<div>
    <label for="ImportedFrom" class="form-label small fw-semibold text-muted text-uppercase mb-1">Source</label>
    <input type="text" id="ImportedFrom" name="ImportedFrom"
           list="importedFromList" value="@Model.ImportedFrom"
           class="form-control form-control-sm" placeholder="Filter by source..." />
    <datalist id="importedFromList">
        <option value="" />
        @foreach (var source in Model.AvailableSources)
        {
            <option value="@source" />
        }
    </datalist>
</div>
```

- `<input list="">` + `<datalist>` = searchable combobox nativo HTML5.
- Opción vacía primero para permitir "clear" (sin valor seleccionado, el form envía ImportedFrom="").
- `form-control form-control-sm` = MDBootstrap consistency.

## Data Flow

```
Browser (GET /transactions)
  │
  ├─1. POST /transactions (form submit con ImportedFrom="nomina.pdf")
  │
  ▼
IndexModel.OnGetAsync()
  │
  ├─2. _mediator.Send(new GetDistinctImportedSourcesQuery())
  │     │
  │     ├─3. TransactionByUserSpecification(userId)
  │     ├─4. repo.FindBySpecificationAsync(spec)
  │     ├─5. LINQ: Select→Where→Distinct→OrderBy→ToList
  │     │
  │     └─6. → List<string> ["facturas.pdf", "nomina.pdf", "recibos.pdf"]
  │
  ├─7. AvailableSources = result
  │
  ├─8. _mediator.Send(new GetTransactionsQuery(..., ImportedFrom="nomina.pdf"))
  │     │
  │     ├─9. Base spec: TransactionByUserSpecification(userId)
  │     ├─10. ImportedFrom != null → AND TransactionByImportedFromSpecification("nomina.pdf")
  │     ├─11. CategoryId? → AND TransactionByCategorySpecification(...)
  │     ├─12. StartDate/EndDate? → AND TransactionByDateRangeSpecification(...)
  │     ├─13. repo.FindBySpecificationAsync(compositeSpec)
  │     ├─14. Sort → Paginate → Map to DTOs
  │     │
  │     └─15. → PaginatedResultDto<TransactionDto>
  │
  ├─16. Model.Transactions = result
  │
  └─17. Render Razor View
         ├─ datalist muestra ["", "facturas.pdf", "nomina.pdf", "recibos.pdf"]
         └─ tabla muestra solo transacciones de nomina.pdf
```

## Interfaces / Contracts

No se definen interfaces nuevas. Los únicos contratos relevantes son:

```csharp
// Query (nuevo)
public record GetDistinctImportedSourcesQuery() : IRequest<List<string>>;

// Handler (nuevo)
public class GetDistinctImportedSourcesQueryHandler
    : IRequestHandler<GetDistinctImportedSourcesQuery, List<string>>;

// Spec (nuevo)
public class TransactionByImportedFromSpecification
    : BaseSpecification<Transaction>;  // criterio: t.ImportedFrom.Equals(value, OrdinalIgnoreCase)
```

## Testing Strategy

| Capa | Qué testear | Enfoque |
|------|------------|---------|
| Unit | `TransactionByImportedFromSpecification` | Comprobar que matchea case-insensitive y que null no matchea |
| Unit | `GetDistinctImportedSourcesQueryHandler` | Mock repositorio devuelve transactions con varios `ImportedFrom`, verificar distinct y orden |
| Integration | Composición de specs en handler | Combinar `TransactionByImportedFromSpecification` con `TransactionByCategorySpecification` vía `CompositeSpecification.And` |

Test scenarios clave:
- RF-1a: case-insensitive match
- RF-1b: null ImportedFrom no matchea
- RF-6a: distinct + orden alfabético
- RF-6b: lista vacía sin transacciones

## Migration / Rollout

No se requiere migración. Cambio puramente aditivo:
1. Crear 3 ficheros nuevos (spec, query, handler)
2. Modificar 4 ficheros existentes (query record, handler, PageModel, Razor)
3. Desplegar todo junto

## Open Questions

None — la spec está completa y los patrones están bien establecidos en el codebase.
