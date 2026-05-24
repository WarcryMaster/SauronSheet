# Proposal: Transaction Source Filter

## Intent

El listado de /transactions muestra `ImportedFrom` ("from filename.pdf") en cada fila, pero no permite filtrar por origen. El usuario necesita un "selector buscador" para reducir la tabla al archivo de origen deseado, siguiendo el patrón de filtros existente (categoría, rango de fechas).

## Scope

### In Scope
- Filtro por `ImportedFrom` en /transactions (mismo formulario que los filtros actuales)
- Searchable combobox para seleccionar el archivo de origen
- Extracción de valores `ImportedFrom` distintos desde el listado cargado (max 1000, limitación aceptada)

### Out of Scope
- Filtro en la página /transactions/search (es otro flujo)
- Método GetDistinctImportedSourcesAsync en repositorio
- Server-side paginación con filtro (consistente con patrón actual in-memory)
- DB index en `ImportedFrom`

## Capabilities

### New Capabilities
- `transaction-source-filter`: Filtro de transacciones por archivo de origen, incluyendo especificación Domain, query handler, PageModel, y searchable combobox en UI

### Modified Capabilities
- None — no existen specs previas para transacciones/filtros

## Approach

1. **Domain**: New `TransactionByImportedFromSpecification` (sigue patrón `TransactionByCategorySpecification`)
2. **Query record**: Add `string? ImportedFrom = null` a `GetTransactionsQuery`
3. **Handler**: Componer spec condicional. Extraer `AvailableSources` del set completo del usuario (no del filtrado) cargado vía `TransactionByUserSpecification`. Retornar ambos vía nuevo `GetTransactionsResult` wrapper
4. **PageModel**: Añadir `ImportedFrom` bind property y `AvailableSources` list. Cargar fuentes desde el query result
5. **Searchable selector**: `<input list="sources">` + `<datalist id="sources">` nativo HTML5 — tipado para filtrar, sin dependencias extra. Consistente con MDBootstrap via `form-control-sm`
6. **Razor page**: Agregar al formulario de filtros existente, entre date range y Apply button

## Affected Areas

| Area | Impact | Description |
|------|--------|-------------|
| `Domain/Specifications/TransactionByImportedFromSpecification.cs` | New | Spec matching t.ImportedFrom == value |
| `Application/Features/Transactions/Queries/GetTransactionsQuery.cs` | Modified | Add ImportedFrom param |
| `Application/Features/Transactions/Queries/GetTransactionsQueryHandler.cs` | Modified | Compose spec + extract sources |
| `Frontend/Pages/Transactions/Index.cshtml.cs` | Modified | Add ImportedFrom, AvailableSources |
| `Frontend/Pages/Transactions/Index.cshtml` | Modified | Add datalist-based searchable filter |

## Risks

None — additive change, no schema or behavior changes.

## Rollback Plan

Revert 5 files. No migration, no data loss.

## Dependencies

None.

## Success Criteria

- [ ] Dropdown muestra archivos de origen distintos del usuario, ordenados alfabéticamente
- [ ] Seleccionar un origen filtra la tabla solo a transacciones de ese archivo
- [ ] "Clear" en el filtro restaura el listado completo
- [ ] Searchable combobox permite tipar para filtrar opciones
- [ ] < 50 líneas C# + < 20 líneas Razor/JS (sin dependencias)
