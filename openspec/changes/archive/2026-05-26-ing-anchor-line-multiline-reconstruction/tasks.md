# Tasks: ING Anchor-Line Multiline Reconstruction

## Review Workload Forecast

| Field | Value |
|-------|-------|
| Estimated changed lines | 200–300 |
| 400-line budget risk | Low |
| Chained PRs recommended | No |
| Suggested split | Single PR |
| Delivery strategy | single-pr-default |
| Chain strategy | pending |

Decision needed before apply: No
Chained PRs recommended: No
Chain strategy: pending
400-line budget risk: Low

### Suggested Work Units

| Unit | Goal | Likely PR | Notes |
|------|------|-----------|-------|
| 1 | RED tests + GREEN assembler + spec update | PR 1 | Base: main. Tests, src, and spec delta in one reviewable unit. |

## Phase 1: Foundation — Actualización de spec

- [x] 1.1 Aplicar el delta IBR-1 a `openspec/specs/ing-block-reconstruction/spec.md`: añadir regla de ancla fuerte y escenarios IBR-1d, IBR-1e, IBR-1f.

## Phase 2: RED — Tests fallidos primero

- [x] 2.1 `IngBlockAssemblerTests.cs` — RED: test `Assemble_AnchorInMiddle_PrependsToPrecedingDescription_IBR1d` (3 líneas nómina); assert: bloque contiene las 3 líneas, transacción anterior sin contaminar.
- [x] 2.2 `IngBlockAssemblerTests.cs` — RED: test `Assemble_AmbiguousBufferForwardToNextAnchor_IBR1e` (bloque A completo + `FRAGMENTO` + bloque B); assert: `FRAGMENTO` en bloque B, bloque A limpio.
- [x] 2.3 `IngBlockAssemblerTests.cs` — RED: test `Assemble_EofBufferNonEmpty_ReappendsToCurrentBlock` (bloque completo + línea sin fecha al final, sin nueva ancla); assert: línea integrada en bloque actual.
- [x] 2.4 `IngBlockAssemblerTests.cs` — RED: test `Assemble_IncompleteBlock_BackwardBehaviorPreserved_IBR1f` (regresión repeated-page-header); assert: línea sin fecha va al bloque previo.
- [x] 2.5 `IngBankPdfParserBlockTests.cs` — RED: test integración `ProcessBlocks_NominaWithAnchor_ProducesCorrectRawTransaction`; assert: `Category = "Nómina"`, `Amount = 2500.00`, DAZN no contaminado.
- [x] 2.6 Ejecutar `dotnet test` y confirmar estado RED en los 3 tests fallidos (IBR-1d, IBR-1e, integración).

## Phase 3: GREEN — Implementación en `IngBlockAssembler`

- [x] 3.1 `IngBlockAssembler.cs` — Declarar variables locales `isComplete` (bool, false) y `ambiguousBuffer` (List\<IngLineData\>) dentro de `Assemble()`.
- [x] 3.2 `IngBlockAssembler.cs` — Añadir helper privado estático `IsStrongAnchor(IngLineData)`: llama a `TryGetBlockStartDate` y `IngMonetaryExtractor.ExtractRightToLeft`; retorna `true` solo si ambos tienen éxito.
- [x] 3.3 `IngBlockAssembler.cs` — Rama `TryGetBlockStartDate == true`: si `IsStrongAnchor` → `FlushBlock`, prepend buffer al nuevo bloque, `isComplete = true`; si no → `FlushBlock`, reanexa buffer al bloque actual, `isComplete = false`.
- [x] 3.4 `IngBlockAssembler.cs` — Rama `TryGetBlockStartDate == false`: si `isComplete` → `ambiguousBuffer.Add(line)`; si no → `currentLines.Add(line)`.
- [x] 3.5 `IngBlockAssembler.cs` — Flush EOF: si `ambiguousBuffer` no vacío → `currentLines.AddRange(ambiguousBuffer)` antes del flush final.
- [x] 3.6 Ejecutar `dotnet test` y confirmar estado GREEN en todos los tests (22/22 IngBlockAssembler + IngBankPdfParserBlock).

## Phase 4: REFACTOR y cierre

- [x] 4.1 REFACTOR: extraer resultado de `IsStrongAnchor` a variable local `strongAnchor` para claridad; sin cambio de comportamiento.
- [x] 4.2 Verificar cobertura: 132/132 Infrastructure Tests, 506/506 solución completa — sin regresiones.
- [x] 4.3 Confirmar que `IngBankPdfParserSingleLineTests` y tests preexistentes de `IngBankPdfParserBlockTests` siguen en verde (132/132).
- [x] 4.4 Ejecutar `dotnet test` final: 0 errores, 0 regresiones (506 tests, 0 failed).
