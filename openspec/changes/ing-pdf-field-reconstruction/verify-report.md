# Verification Report: ING PDF Field Reconstruction

> **BASELINE PRE-IMPLEMENTACIÓN** — No existe apply-progress para este cambio.
> Este reporte documenta el estado actual del repositorio frente a los artefactos
> SDD recién escritos (proposal, spec, design, tasks). NO hay implementación iniciada.
> Ninguna tarea ha comenzado. El veredicto es FAIL esperado por diseño.

---

## Cabecera

| Campo | Valor |
|-------|-------|
| Change | `ing-pdf-field-reconstruction` |
| Modo | Pre-implementación (baseline gap report) |
| Strict TDD | ✅ ACTIVO — runner: `dotnet test` |
| Artifact store | Hybrid (Engram + OpenSpec filesystem) |
| Delivery | force-chained / feature-branch-chain |
| Budget | 400 líneas por PR |
| Fecha | 2026-05-25 |
| Rama activa | `master` |

---

## Completitud de Tareas

| Fase | Tarea | Estado | Tipo |
|------|-------|--------|------|
| 1 | 1.1 [RED] Crear `IngBlockAssemblerTests.cs` (IBR-1a/1b/1c) | ❌ No iniciada | Core |
| 1 | 1.2 [GREEN] Crear `IngBlock.cs` y `IngBlockAssembler.cs` | ❌ No iniciada | Core |
| 1 | 1.3 [RED] Crear `IngMonetaryExtractorTests.cs` (IBR-2a/2b/IBR-4a) | ❌ No iniciada | Core |
| 1 | 1.4 [GREEN] Crear `IngMonetaryExtractor.cs` | ❌ No iniciada | Core |
| 1 | 1.5 [REFACTOR] Nullability, XML doc, dotnet test verde | ❌ No iniciada | Core |
| 2 | 2.1 [RED] Crear `IngBankPdfParserBlockTests.cs` (fixtures DAZN/parking/nómina) | ❌ No iniciada | Core |
| 2 | 2.2 [RED] Tests de detección por cabecera AdaptivePdfParser (IBR-5a/5b) | ❌ No iniciada | Core |
| 2 | 2.3 [GREEN] Modificar `IngBankPdfParser.cs` — pipeline block-first | ❌ No iniciada | Core |
| 2 | 2.4 [GREEN] Modificar `AdaptivePdfParser.cs` — `HasIngHeader` | ❌ No iniciada | Core |
| 2 | 2.5 [REFACTOR] Eliminar `IngTransactionLineParser.cs` y sus tests | ❌ No iniciada | Core |
| 3 | 3.1 [RED] Crear `IngControlledTaxonomyTests.cs` (IBR-3a/3b/3c, PCE-1a/1b/1c/1d) | ❌ No iniciada | Core |
| 3 | 3.2 [GREEN] Crear `IngControlledTaxonomy.cs` | ❌ No iniciada | Core |
| 3 | 3.3 [GREEN] Cablear `IngControlledTaxonomy` en `IngBankPdfParser.cs` | ❌ No iniciada | Core |
| 3 | 3.4 [REFACTOR] Cobertura ≥ 70 %, validar paths RawOnly/null | ❌ No iniciada | Core |
| 3 | 3.5 Modificar `openspec/specs/pdf-category-extraction/spec.md` (delta PCE-1) | ❌ No iniciada | Core |

**Completitud: 0/15 tareas (0 %) — Ninguna tarea iniciada.**

---

## Evidencia de Build y Tests

### Build (`dotnet build --no-restore -v minimal`)

```
Compilación correcta.
    0 Advertencia(s)
    0 Errores
Tiempo transcurrido 00:00:07.43
```

**Resultado: ✅ BUILD LIMPIO**

### Tests (`dotnet test`)

| Proyecto | Correctas | Incorrectas | Omitidas | Total |
|----------|-----------|-------------|----------|-------|
| Infrastructure.Tests | 93 | 0 | 0 | 93 |
| Domain.Tests | 190 | 0 | 0 | 190 |
| Application.Tests | 150 | 0 | 0 | 150 |
| Frontend.Tests | 27 | 0 | 0 | 27 |
| Integration.Tests | 7 | 0 | 0 | 7 |
| **TOTAL** | **467** | **0** | **0** | **467** |

**Resultado: ✅ TODOS LOS TESTS EXISTENTES PASAN (467/467)**

> Nota: estos tests pertenecen a funcionalidad ANTERIOR al cambio. No cubren ningún requisito IBR-* ni el delta PCE-1.

---

## TDD Compliance (Strict TDD Mode)

| Check | Resultado | Detalle |
|-------|-----------|---------|
| TDD Evidence reportada (apply-progress) | ❌ No existe | No hay apply-progress — implementación no iniciada |
| Todos los tasks tienen test files | ❌ 0/15 | Ningún fichero de test del cambio creado |
| RED confirmado (test files existen) | ❌ 0/15 | Ficheros de test no existen |
| GREEN confirmado (tests pasan) | ❌ 0/15 | Sin tests nuevos que ejecutar |
| Triangulación adecuada | ➖ N/A | No aplica — pre-implementación |
| Safety Net para ficheros modificados | ➖ N/A | No aplica — pre-implementación |

**TDD Compliance: 0/6 checks — PRE-IMPLEMENTACIÓN (esperado)**

---

## Distribución de Test Layers

| Layer | Tests nuevos | Ficheros nuevos | Tools |
|-------|-------------|-----------------|-------|
| Unit (nuevos — requeridos) | 0 | 0 | xUnit |
| Integration (nuevos — requeridos) | 0 | 0 | xUnit |
| E2E | 0 | 0 | No aplica a este cambio |
| **Existentes sin relación** | **467** | **~65** | xUnit + Moq |

---

## Cobertura de Ficheros Cambiados

**No calculable** — ningún fichero del cambio existe aún. Los ficheros del cambio no están creados.

---

## Calidad de Aserciones

**No aplica** — no existen ficheros de test para este cambio.  
**Assertion quality**: ➖ Pre-implementación — no hay tests que auditar.

---

## Quality Metrics

**Linter/Analyzer**: ✅ 0 errores, 0 advertencias (compilación limpia)  
**Type Checker**: ✅ 0 errores de tipo

---

## Matriz de Cumplimiento de Specs

### Spec: ing-block-reconstruction (IBR)

| Escenario | Descripción | Test en repo | Estado |
|-----------|-------------|--------------|--------|
| IBR-1a | Fila única → 1 bloque, Amount=-12.99, Balance=1234.56 | — | ❌ UNTESTED |
| IBR-1b | Línea sin fecha → adjuntada al bloque previo | — | ❌ UNTESTED |
| IBR-1c | Dos fechas → dos bloques independientes | — | ❌ UNTESTED |
| IBR-2a | R→L sobre texto terminado en `…-12,99 1.234,56` → Amount/Balance | — | ❌ UNTESTED |
| IBR-2b | Cualquier fila ING → Comment=null | — | ❌ UNTESTED |
| IBR-3a | Texto `Compras Online DAZN` L→R → Cat/SubCat/Desc correctos | — | ❌ UNTESTED |
| IBR-3b | `Inversiones Fondos Pago especial` → source=RawOnly, no null | — | ❌ UNTESTED |
| IBR-3c | Descripción no contiene importe/saldo/cat/subcat | — | ❌ UNTESTED |
| IBR-4a | R→L no produce 2 tokens numéricos → parser retorna null | — | ❌ UNTESTED |
| IBR-4b | Importe aislable + cat desconocida → RawOnly, no null | — | ❌ UNTESTED |
| IBR-5a | Cabecera `F. VALOR` + `CATEGORÍA` → selecciona IngBankPdfParser | — | ❌ UNTESTED |
| IBR-5b | Cabecera sin esos tokens → NO selecciona IngBankPdfParser | — | ❌ UNTESTED |

**IBR cumplidos: 0/12 — todos UNTESTED**

### Spec delta: pdf-category-extraction (PCE-1 path ING)

| Escenario | Descripción | Test en repo | Estado |
|-----------|-------------|--------------|--------|
| PCE-1a | ING: `Inversiones Fondos` ausente en taxonomía → RawOnly | — | ❌ UNTESTED |
| PCE-1b | ING: sin categoría detectable → Category=null | — | ❌ UNTESTED |
| PCE-1c | ING: `Compras` sin subcategoría → SubCategory=null | — | ❌ UNTESTED |
| PCE-1d | No-ING: categoría como literal sin lista cerrada | — | ❌ UNTESTED |

**PCE-1 delta cumplidos: 0/4 — todos UNTESTED**

---

## Tabla de Correctitud (Ficheros del Cambio)

| Fichero | Acción Diseño | Estado Actual | Gap |
|---------|---------------|---------------|-----|
| `IngBlock.cs` | Crear | ❌ Ausente | No creado |
| `IngBlockAssembler.cs` | Crear | ❌ Ausente | No creado |
| `IngMonetaryExtractor.cs` | Crear | ❌ Ausente | No creado |
| `IngControlledTaxonomy.cs` | Crear | ❌ Ausente | No creado |
| `IngBankPdfParser.cs` | Modificar (pipeline block-first) | ⚠️ Sin modificar | Sigue dual-path con FlushRowBuffer/ParseMultiLine/ParseIngTransactionLine |
| `AdaptivePdfParser.cs` | Modificar (HasIngHeader) | ⚠️ Sin modificar | Sigue parse-then-count — contradice IBR-5 |
| `IngTransactionLineParser.cs` | Eliminar (Phase 2) | ⚠️ Aún existe | No eliminado |
| `IngColumnThresholds.cs` | Mantener como fallback | ✅ Existe | OK |
| `IngBlockAssemblerTests.cs` | Crear | ❌ Ausente | No creado |
| `IngMonetaryExtractorTests.cs` | Crear | ❌ Ausente | No creado |
| `IngControlledTaxonomyTests.cs` | Crear | ❌ Ausente | No creado |
| `IngBankPdfParserBlockTests.cs` | Crear | ❌ Ausente | No creado |
| `IngTransactionLineParserTests.cs` | Eliminar (Phase 2) | ⚠️ Aún existe | No eliminado |
| `openspec/specs/pdf-category-extraction/spec.md` | Modificar (delta PCE-1) | ⚠️ Baseline pre-delta | Muestra PCE-1 antiguo (sin distinción ING/no-ING) |

---

## Coherencia de Diseño

| Decisión de Diseño | ¿Implementada? | Notas |
|---|---|---|
| Pipeline unificado block-first (assemble→R→L→L→R→emit) | ❌ No | IngBankPdfParser sigue dual-path |
| R→L monetary extraction sobre bloque completo | ❌ No | `ExtractTrailingNumbers` extrae todos los tokens numéricos |
| `IngControlledTaxonomy` ordered dict L→R | ❌ No | Clase no existe |
| Header-based ING detection O(1 páginas) | ❌ No | `IsIngFormatAsync` parsea PDF completo |
| `IngColumnThresholds` degradado a fallback | ⚠️ Parcial | Clase existe, sigue siendo el path principal |
| `IngTransactionLineParser` eliminado | ❌ No | Clase existe y activa en ParseMultiLineTransaction |

---

## Problemas Encontrados

### CRITICAL

| # | Área | Descripción |
|---|------|-------------|
| C-1 | TDD / Strict Mode | Ningún fichero de test del cambio existe. **0/12 escenarios IBR tienen test. 0/4 escenarios PCE-1 delta tienen test.** La implementación no puede comenzar en Strict TDD sin RED state. |
| C-2 | IBR-5a/5b | `AdaptivePdfParser.IsIngFormatAsync` usa `_ingParser.ParseAsync(ms) is { Count: > 0 }` — parsea el PDF completo para detectar el formato. Contradice directamente IBR-5 y la decisión de diseño de header-scan O(1 páginas). El método `HasIngHeader(Stream)` no existe. |
| C-3 | IBR-1/2/3/4 | `IngBlock`, `IngBlockAssembler`, `IngMonetaryExtractor`, `IngControlledTaxonomy` no existen. El pipeline block-first no está implementado. |
| C-4 | OpenSpec baseline | `openspec/specs/pdf-category-extraction/spec.md` muestra el PCE-1 antiguo. La delta del cambio está solo en la carpeta del cambio, no aplicada al baseline. Bloqueará el paso de archivado. |

### WARNING

| # | Área | Descripción |
|---|------|-------------|
| W-1 | `IngTransactionLineParser.cs` | Sigue activo en producción, referenciado por `ParseMultiLineTransaction`. El diseño lo marca para eliminación en task 2.5. Riesgo de deuda técnica post-implementación si no se elimina en Phase 2. |
| W-2 | `IngColumnThresholds.cs` | El diseño lo degrada a fallback secundario, pero actualmente es el path principal del single-line. Verificar que `IngBankPdfParserSingleLineTests` y `IngColumnThresholdsTests` sigan pasando tras el cableado del pipeline en Phase 2. |
| W-3 | Fixtures físicas de PDF | La tarea 2.1 requiere fixtures de enero 2025 (DAZN, parking, nómina). No hay fixtures de PDF binarios en el repo. Confirmar que los tests se basen en texto reconstruido (sin PDF físico) o que los fixtures se añadan a una carpeta `tests/fixtures/`. |
| W-4 | `IngControlledTaxonomy` — lista inicial | La propuesta indica "seed inicial desde fixture enero 2025", pero la lista controlada no está completamente definida (open question en el diseño). Riesgo de IBR-3a fallando si DAZN/parking/nómina no están en la lista inicial. |

### SUGGESTION

| # | Área | Descripción |
|---|------|-------------|
| S-1 | `ParseMultiLineTransaction` → thresholds null | La llamada `ParseIngTransactionLine(lines[0], rowNumber, thresholds: null)` bypasea `IngColumnThresholds` deliberadamente. Al cablear el pipeline, verificar que esta lógica no quede zombie. |
| S-2 | `IngBankPdfParserBlockTests.cs` — scope | El diseño etiqueta estos tests como "Integration". Documentar si los fixtures son texto reconstruido (unit) o bytes de PDF real (integration) para coherencia de nomenclatura. |
| S-3 | Comment = null contractual | IBR-2b requiere `Comment = null` siempre. El código pasa `null` explícitamente pero no hay test unitario que lo verifique. Incluir en IBR-2b test para evitar regresión futura. |

---

## Veredicto

### ❌ FAIL — BASELINE PRE-IMPLEMENTACIÓN

> Este es el resultado **esperado y correcto** para una verificación ejecutada ANTES de comenzar la implementación.
>
> El repositorio se encuentra en estado limpio (build ✅, 467/467 tests ✅, 0 regresiones) y los artefactos SDD (proposal, spec, design, tasks) están escritos y listos. Ninguna tarea del cambio ha comenzado.
>
> El veredicto FAIL refleja el gap entre el estado actual del código y los 16 requisitos definidos (12 IBR + 4 PCE-1 delta) — ese gap ES exactamente el trabajo pendiente.
>
> **Para pasar a PASS:** ejecutar `sdd-apply` siguiendo el orden de fases (PR 1 → PR 2 → PR 3) con Strict TDD activo.

---

## Resumen de Gaps por Fase / PR

| Fase / PR | Gap principal | Ficheros faltantes clave |
|-----------|--------------|--------------------------|
| PR 1 — helpers puros | IngBlockAssembler + IngMonetaryExtractor no existen | `IngBlock.cs`, `IngBlockAssembler.cs`, `IngMonetaryExtractor.cs`, `IngBlockAssemblerTests.cs`, `IngMonetaryExtractorTests.cs` |
| PR 2 — cableado | Pipeline no cableado; detection usa parse-then-count | Mods `IngBankPdfParser.cs`, `AdaptivePdfParser.cs`; `IngBankPdfParserBlockTests.cs`, tests IBR-5 |
| PR 3 — taxonomía + delta | IngControlledTaxonomy no existe; OpenSpec baseline desactualizado | `IngControlledTaxonomy.cs`, `IngControlledTaxonomyTests.cs`; mod `pdf-category-extraction/spec.md` |
