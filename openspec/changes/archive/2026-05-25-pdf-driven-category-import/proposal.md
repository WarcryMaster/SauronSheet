# Proposal: PDF-Driven Category Import

## Intent

El import actual pierde categorías del PDF porque `IngBankPdfParser` compara contra listas hardcodeadas.
La fuente de verdad debe ser el literal del PDF: capturar siempre, normalizar solo para deduplicación,
y crear categorías/subcategorías via get-or-add por usuario — sin categorías predefinidas.

## Scope

### In Scope
- Corregir `IngBankPdfParser` para extraer literales de categoría/subcategoría sin listas cerradas
- Introducir servicio get-or-add PDF-driven que resuelve o crea categorías/subcategorías por usuario
- Clave normalizada en Application para deduplicación robusta (no en DB por ahora)
- Constraint UNIQUE o RPC SQL en Supabase para garantizar get-or-add seguro
- Persistir `bank_category`/`bank_subcategory` (raw) + IDs asociados en `transactions`
- Excluir categorías predefinidas/system defaults del flujo de import PDF
- `TransactionCategoryDisplayHelper` prioriza raw PDF values para transacciones importadas

### Out of Scope
- Backfill de transacciones históricas (diferido a cambio separado)
- Recategorización manual de importaciones anteriores
- Otros parsers distintos de ING Bank

## Capabilities

> Contrato con la fase sdd-spec. Verificado contra `openspec/specs/`.

### New Capabilities
- `pdf-category-extraction`: Extracción fiable de categoría/subcategoría del PDF sin listas cerradas;
  literal del PDF como única fuente de verdad; incluye normalización para deduplicación.

### Modified Capabilities
- `category-resolution`: Servicio evoluciona de lookup-only a get-or-add PDF-driven;
  system defaults excluidos del import path; UI prioriza raw PDF sobre nombre resuelto
  para transacciones importadas.

## Approach

Enfoque evolutivo con núcleo limpio: (1) parser ING elimina `KnownCategories`/`KnownSubCategories`
y preserva literales, (2) nuevo `PdfCategoryResolverService` hace get-or-add con clave normalizada,
(3) `ImportTransactionsFromPdfCommandHandler` usa el nuevo servicio, (4) display helper actualizado.

`ICategoryResolutionService` actual se mantiene para el path de recategorización manual.
El nuevo servicio PDF-driven es exclusivo del import path — sin mezclar semánticas.

## Affected Areas

| Area | Impact | Description |
|------|--------|-------------|
| `Infrastructure/PDF/Parsers/IngBankPdfParser.cs` | Modified | Eliminar listas cerradas; extraer literales del PDF |
| `Application/Features/Transactions/Commands/ImportTransactionsFromPdfCommandHandler.cs` | Modified | Usar PdfCategoryResolverService |
| `Application/Services/PdfCategoryResolverService.cs` | New | Get-or-add con clave normalizada |
| `Domain/Entities/Category.cs` + `CategoryService.cs` | Modified | Filtrar system defaults del import |
| `Frontend/Helpers/TransactionCategoryDisplayHelper.cs` | Modified | Priorizar bank_category/bank_subcategory para importadas |
| Supabase migration | New | UNIQUE constraint o RPC get-or-add en categories/subcategories |

## Risks

| Risk | Likelihood | Mitigation |
|------|------------|------------|
| Condición de carrera en get-or-add | Med | UNIQUE constraint + ON CONFLICT DO NOTHING o función SQL atómica |
| Normalización inconsistente entre releases | Med | Función C# estática con tests; toda la lógica en un único lugar |
| Regresión en display de transacciones manuales | Low | Slice UI separado; tests Before/After sobre helper |
| Legado system defaults filtra al import | Med | Guard explícito en resolver; test SD-regression |

## Rollback Plan

La columna `bank_category` ya existe en `transactions`; revertir al servicio previo es un
rollback de código sin migración destructiva. El slice de parser puede revertirse de forma
independiente. El UNIQUE constraint no tiene impacto negativo en datos existentes.

## Dependencies

- Constraint UNIQUE o función RPC SQL en Supabase para subcategorías (migration requerida)

## Success Criteria

- [ ] `IngBankPdfParser` preserva literales en PDFs con categorías fuera de las listas previas
- [ ] Import sin categorías preexistentes crea las categorías correctas por usuario
- [ ] No se crean duplicados bajo imports concurrentes del mismo archivo
- [ ] UI muestra valores raw del PDF para transacciones importadas
- [ ] Tests Domain + Application verdes; build sin warnings; suite completa verde
