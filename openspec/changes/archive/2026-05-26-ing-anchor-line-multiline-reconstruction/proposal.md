# Proposal: ING Anchor-Line Multiline Reconstruction

## Intent

`IngBlockAssembler` asigna toda línea sin fecha al bloque anterior de forma incondicional. Esta
suposición falla cuando el PDF ING presenta el patrón `descripción-sin-fecha → ancla(fecha+importe+saldo) → descripción-sin-fecha`
(ej. nómina fragmentada): los fragmentos previos a la ancla se asignan a la transacción anterior,
corrompiendo importe y descripción. IBR-1 de la spec vigente codifica exactamente esa suposición
refutada y necesita un delta.

## Scope

### In Scope
- Rediseño mínimo de `IngBlockAssembler.Assemble(...)` con buffer ambiguo y detección de ancla fuerte.
- Delta de spec para IBR-1 en `ing-block-reconstruction` (la regla de "siempre hacia atrás" ya no es incondicional).
- Tests unitarios nuevos: ancla en medio, reasignación de línea hacia el siguiente bloque, regresión repeated-page-header.

### Out of Scope
- Re-segmentación geométrica por coordenadas X/Y del PDF.
- Cambios en `IngControlledTaxonomy` (etiquetado semántico de nómina queda para otro cambio).
- Modificaciones en `AdaptivePdfParser` o detección de cabecera.
- Parsers de bancos distintos a ING.

## Capabilities

> Esta sección es el contrato entre la propuesta y la fase de specs.
> `sdd-spec` debe leer esto para saber qué spec crear o actualizar.

### New Capabilities
None

### Modified Capabilities
- `ing-block-reconstruction`: IBR-1 deja de afirmar que TODA línea sin fecha va al bloque previo;
  se introduce la excepción de ancla fuerte con buffer ambiguo.

## Approach

**Approach 2 — ensamblado anchor-aware con buffer ambiguo** (recomendado en la exploración):

- **Ancla fuerte**: línea que supera `TryGetBlockStartDate(...)` Y permite aislar par importe/saldo
  mediante `IngMonetaryExtractor.ExtractRightToLeft(...)` en esa misma línea.
- Bloque abierto **incompleto** (sin importe/saldo aislables) → las líneas sin fecha siguen al bloque
  previo. Preserva el caso repeated-page-header ya corregido.
- Bloque abierto **completo** → las líneas sin fecha van al **buffer ambiguo**.
  - Llega nueva ancla fuerte: el buffer se antepone al nuevo bloque.
  - Llega EOF sin nueva ancla: el buffer se reanexa al bloque actual (sin pérdida de texto).

## Affected Areas

| Área | Impacto | Descripción |
|------|---------|-------------|
| `src/…/PDF/Parsers/IngBlockAssembler.cs` | Modificado | Lógica central; introduce buffer ambiguo y detección de ancla fuerte |
| `src/…/PDF/Parsers/IngBankPdfParser.cs` | Modificado menor | Ajuste de contrato/cableado si la firma de `Assemble` cambia |
| `tests/…/PDF/Parsers/IngBlockAssemblerTests.cs` | Modificado | Casos nuevos: ancla en medio, reasignación hacia delante |
| `tests/…/PDF/Parsers/IngBankPdfParserBlockTests.cs` | Modificado | Nómina con ancla + regresión repeated-page-header |
| `openspec/specs/ing-block-reconstruction/spec.md` | Modificado | Delta IBR-1 |

## Risks

| Riesgo | Probabilidad | Mitigación |
|--------|-------------|------------|
| Buffer roba continuaciones legítimas del bloque anterior | Media | Activar solo cuando el bloque está completo; ancla fuerte exige par importe/saldo en la misma línea |
| Regresión en repeated-page-header | Baja | Tests de regresión explícitos obligatorios antes de implementar |
| Taxonomía de nómina sigue parcial tras el reagrupado | Baja | Este cambio no promete resolver etiquetado semántico; se documenta como fuera de alcance |

## Rollback Plan

`git revert` del PR. Sin cambios de esquema, migraciones ni dependencias externas.

## Dependencies

Ninguna externa. Reutiliza `IngMonetaryExtractor.ExtractRightToLeft(...)` para la detección de ancla
fuerte, ya disponible en Infrastructure.

## Success Criteria

- [ ] Test de nómina con ancla en medio produce la transacción correcta sin contaminar la fila anterior.
- [ ] Los tests de regresión repeated-page-header siguen en verde tras el cambio.
- [ ] `dotnet test` pasa sin degradación de cobertura en la capa Infrastructure.
- [ ] IBR-1 en `openspec/specs/ing-block-reconstruction/spec.md` refleja la nueva regla con excepción de ancla fuerte.
