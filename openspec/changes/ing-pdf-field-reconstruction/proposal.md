# Proposal: ING PDF Field Reconstruction

## Intent

El parser ING actual mezcla tres responsabilidades —ensamblado de bloque, extracción numérica e identificación de campos— y usa un contrato frágil por posición de línea que fusiona filas adyacentes y mete categoría/subcategoría dentro de descripción. `ing-single-line-category-extraction` solo mejoró el split geométrico para el caso single-line; el bug estructural está en el ensamblado de bloques lógicos. Este cambio reemplaza ese foco por un modelo **block-first** fiel al contrato real del export ING de enero 2025:

> `F. VALOR | CATEGORÍA | SUBCATEGORÍA | DESCRIPCIÓN | COMENTARIO | IMPORTE | SALDO`

`COMENTARIO` siempre vacío; descripción final = solo campo PDF. Filas multilinea: líneas sin fecha inicial continúan el movimiento previo. Extracción: importe/saldo derecha-a-izquierda; categoría/subcategoría izquierda-a-derecha.

## Scope

### In Scope
- Rediseño de `IngBankPdfParser` con ensamblado formal de bloque lógico por transacción
- Extractor de cola monetaria derecha-a-izquierda (importe + saldo)
- Extractor de taxonomía izquierda-a-derecha con lista controlada ING (`IngControlledTaxonomy`)
- Fallback conservador: null si el bloque no permite extracción segura
- Hardening de detección ING en `AdaptivePdfParser` (por cabecera, no por row count)
- Fixtures de enero 2025: DAZN, parking, nómina, filas adyacentes incorrectamente ensambladas
- Delta spec de `pdf-category-extraction` para acotar PCE-1 al path ING

### Out of Scope
- Otros bancos/formatos PDF (Santander, BBVA, genérico)
- Migración de transacciones ya importadas
- UI para visualizar o editar la taxonomía ING

## Capabilities

### New Capabilities
- `ing-block-reconstruction`: ensamblado de bloque lógico por transacción ING desde líneas físicas (multi-línea, continuación sin fecha, extracción numérica R→L, taxonomía L→R, fallback conservador)

### Modified Capabilities
- `pdf-category-extraction`: delta PCE-1 — permite lista controlada ING como fuente primaria de categoría/subcategoría; valores desconocidos se siguen preservando como `RawOnly`

## Approach

**Approach 2 — Rediseño block-first** (menor riesgo arquitectónico; sin cambios en Application ni Domain):

| Stage | Acción |
|-------|--------|
| 1 · Block assembly | Formalizar bloque lógico único por transacción; unir continuaciones antes de extraer |
| 2 · Extracción R→L | Reemplazar `ExtractTrailingNumbers` por extractor de cola monetaria sobre el bloque completo |
| 3 · Taxonomía L→R | Sobre texto limpio de números: consumir categoría/subcategoría con `IngControlledTaxonomy`; resto = descripción; `COMENTARIO` = null |
| 4 · Fallback | Si no se aíslan importe+fecha → null; si taxonomía no reconocida → Category/SubCategory null, descripción limpia |
| 5 · Detection | `AdaptivePdfParser` detecta ING por cabecera `F. VALOR | CATEGORÍA`, independiente de row count |

**Chained PRs previstas (budget 400 líneas):**
- **PR 1**: fixtures enero 2025 + helpers puros (ensamblado de bloque, extractor numérico R→L)
- **PR 2**: cableado en `IngBankPdfParser` + fallback + hardening `AdaptivePdfParser`
- **PR 3**: `IngControlledTaxonomy` + regresiones nómina/DAZN/parking + delta OpenSpec

## Affected Areas

| Área | Impacto | Descripción |
|------|---------|-------------|
| `Infrastructure/PDF/Parsers/IngBankPdfParser.cs` | Modified | Núcleo del rediseño block-first |
| `Infrastructure/PDF/Parsers/IngTransactionLineParser.cs` | Modified | Reducido a helper o eliminado |
| `Infrastructure/PDF/Parsers/IngColumnThresholds.cs` | Modified | Degradado a fallback, no fuente principal |
| `Infrastructure/PDF/Parsers/AdaptivePdfParser.cs` | Modified | Detección por cabecera |
| `Infrastructure/PDF/Parsers/IngControlledTaxonomy.cs` | New | Taxonomía ING controlada |
| `tests/Infrastructure.Tests/PDF/Parsers/` | Modified | Nuevos fixtures, cobertura con casos reales |
| `openspec/specs/pdf-category-extraction/spec.md` | Modified | Delta PCE-1 para path ING |

## Risks

| Riesgo | Prob. | Mitigación |
|--------|-------|------------|
| Detección falsa no-ING con parser estricto | Med | Stage 5 desacopla detección de row count |
| Taxonomía incompleta → `RawOnly` legítimos | Med | Fallback preserva descripción; lista controlada incremental |
| Fallback agresivo → filas perdidas | Low | Fixtures reales de enero 2025 lo verifican antes del merge |
| Arrastre de `IngColumnThresholds` como fuente principal | Low | Degradado a fallback explícitamente en Stage 2 |

## Rollback Plan

Todos los cambios son en Infrastructure; no hay migraciones de BD ni cambios en Application/Domain. Las PR slices se revierten de forma independiente. La delta OpenSpec se introduce en PR 3 (última); las PRs 1–2 no rompen el spec vigente. Revertir PR 3 restaura el estado del spec sin afectar el código.

## Dependencies

- Fixtures de PDF de enero 2025 con casos reales (DAZN, parking, nómina, filas adyacentes)
- Confirmación del usuario de la lista controlada de categorías/subcategorías ING

## Success Criteria

- [ ] Filas DAZN, parking y nómina de enero 2025 importan con categoría + subcategoría correctas
- [ ] Filas multilinea no se fusionan con la transacción adyacente
- [ ] `COMENTARIO` siempre `null` en el path ING
- [ ] `AdaptivePdfParser` detecta ING por cabecera, sin depender de parseo exitoso
- [ ] Todos los tests existentes pasan sin regresión
- [ ] Cobertura de Infrastructure ≥ 70 %
