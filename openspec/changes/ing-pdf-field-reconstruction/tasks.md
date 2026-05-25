# Tasks: ING PDF Field Reconstruction

## Review Workload Forecast

| Field | Value |
|-------|-------|
| Estimated changed lines | ~900 total (3 × ≤350 LOC por PR) |
| 400-line budget risk | Low (por PR individual) |
| Chained PRs recommended | Yes |
| Suggested split | PR 1 → PR 2 → PR 3 |
| Delivery strategy | force-chained |
| Chain strategy | feature-branch-chain |

Decision needed before apply: No
Chained PRs recommended: Yes
Chain strategy: feature-branch-chain
400-line budget risk: Low

### Suggested Work Units

| Unit | Goal | Likely PR | Notes |
|------|------|-----------|-------|
| 1 | Pure helpers (IngBlock, IngBlockAssembler, IngMonetaryExtractor) + unit tests IBR-1/IBR-2 | PR 1 | Base: `feature/ing-pdf-field-reconstruction` |
| 2 | Pipeline wiring IngBankPdfParser + AdaptivePdfParser header detection | PR 2 | Base: branch de PR 1; tests IBR-4, IBR-5 |
| 3 | IngControlledTaxonomy + fixtures regresión + delta OpenSpec PCE-1 | PR 3 | Base: branch de PR 2; tests IBR-3, PCE-1 |

---

## Phase 1 — Helpers Puros (PR 1)
> Base: `feature/ing-pdf-field-reconstruction`

- [x] 1.1 [RED] Crear `tests/SauronSheet.Infrastructure.Tests/PDF/Parsers/IngBlockAssemblerTests.cs` con casos IBR-1a, IBR-1b, IBR-1c en rojo
- [x] 1.2 [GREEN] Crear `src/SauronSheet.Infrastructure/PDF/Parsers/IngBlock.cs` (`internal readonly record struct`) y `IngBlockAssembler.cs` (static, date-delimiter `dd/mm/yyyy`)
- [x] 1.3 [RED] Crear `tests/SauronSheet.Infrastructure.Tests/PDF/Parsers/IngMonetaryExtractorTests.cs` con casos IBR-2a, IBR-2b, IBR-4a en rojo
- [x] 1.4 [GREEN] Crear `src/SauronSheet.Infrastructure/PDF/Parsers/IngMonetaryExtractor.cs` (static R→L; retorna `null` si < 2 tokens numéricos aislables)
- [x] 1.5 [REFACTOR] Revisar nullability, XML doc, `ConfigureAwait(false)` en helpers; ejecutar `dotnet test` — todos los tests en verde

---

## Phase 2 — Cableado del Pipeline + Detección (PR 2)
> Base: branch `slice/ing-pdf-field-reconstruction-pr1`

- [ ] 2.1 [RED] Crear `tests/SauronSheet.Infrastructure.Tests/PDF/Parsers/IngBankPdfParserBlockTests.cs`: fixtures DAZN, parking, nómina, fila multilinea; caso IBR-4b (fallback conservador) en rojo
- [ ] 2.2 [RED] Añadir tests de detección por cabecera en `AdaptivePdfParser` (IBR-5a, IBR-5b) en rojo
- [ ] 2.3 [GREEN] Modificar `src/SauronSheet.Infrastructure/PDF/Parsers/IngBankPdfParser.cs`: eliminar `FlushRowBuffer`, `ParseMultiLineTransaction`, `ParseIngTransactionLine`; cablear pipeline `IngBlockAssembler → IngMonetaryExtractor`
- [ ] 2.4 [GREEN] Modificar `src/SauronSheet.Infrastructure/PDF/Parsers/AdaptivePdfParser.cs`: añadir `HasIngHeader(Stream)`, reemplazar detección por row-count con header-scan O(1 páginas)
- [ ] 2.5 [REFACTOR] Eliminar `src/SauronSheet.Infrastructure/PDF/Parsers/IngTransactionLineParser.cs` y `tests/SauronSheet.Infrastructure.Tests/PDF/IngTransactionLineParserTests.cs`; `dotnet test` en verde

---

## Phase 3 — Taxonomía + Delta Spec (PR 3)
> Base: branch `slice/ing-pdf-field-reconstruction-pr2`

- [ ] 3.1 [RED] Crear `tests/SauronSheet.Infrastructure.Tests/PDF/Parsers/IngControlledTaxonomyTests.cs` con casos IBR-3a, IBR-3b, IBR-3c, PCE-1a, PCE-1b, PCE-1c, PCE-1d en rojo
- [ ] 3.2 [GREEN] Crear `src/SauronSheet.Infrastructure/PDF/Parsers/IngControlledTaxonomy.cs` (ordered dict L→R; `ExtractLeftToRight(text)` → `(cat?, subCat?, description)`; initial seed desde fixture enero 2025)
- [ ] 3.3 [GREEN] Cablear `IngControlledTaxonomy.ExtractLeftToRight` en `IngBankPdfParser.cs` pipeline (tras `IngMonetaryExtractor`; source `RawOnly` si categoría no reconocida)
- [ ] 3.4 [REFACTOR] Validar cobertura Infrastructure ≥ 70 %; revisar todos los paths `RawOnly` vs. null; `dotnet test` verde
- [ ] 3.5 Modificar `openspec/specs/pdf-category-extraction/spec.md`: aplicar delta PCE-1 (path ING usa `IngControlledTaxonomy`; path no-ING sin lista cerrada)
