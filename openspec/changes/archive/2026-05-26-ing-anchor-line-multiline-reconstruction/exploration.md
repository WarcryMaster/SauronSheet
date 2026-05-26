## Exploration: ing-anchor-line-multiline-reconstruction

### Current State
`IngBankPdfParser.ParseAsync(...)` reconstruye líneas por página, aplica `StripLeadingRepeatedPageHeaderSection(...)` en páginas posteriores, concatena `sanitizedLines`, localiza la primera cabecera ING y pasa todas las líneas de datos a `ProcessBlocks(...)` (`src/SauronSheet.Infrastructure/PDF/Parsers/IngBankPdfParser.cs`).

Hoy los movimientos lógicos se forman en `IngBlockAssembler.Assemble(...)`: si una línea empieza con fecha en la primera columna abre bloque; si no empieza con fecha se adjunta SIEMPRE al bloque abierto más reciente; y si aparece antes de la primera fecha se ignora (`src/SauronSheet.Infrastructure/PDF/Parsers/IngBlockAssembler.cs`). Esa regla sigue asumiendo que toda línea sin fecha pertenece hacia atrás, así que falla con el patrón confirmado de ancla en medio: descripción sin fecha + línea ancla con fecha/importe/saldo + descripción sin fecha.

`ProcessBlocks(...)` después aplica `IngMonetaryExtractor.ExtractRightToLeft(...)` sobre el bloque completo y `IngControlledTaxonomy.ExtractLeftToRight(...)` sobre el texto limpio (`src/SauronSheet.Infrastructure/PDF/Parsers/IngBankPdfParser.cs`, `src/SauronSheet.Infrastructure/PDF/Parsers/IngMonetaryExtractor.cs`, `src/SauronSheet.Infrastructure/PDF/Parsers/IngControlledTaxonomy.cs`). La avería nueva NO está en `AdaptivePdfParser` ni en la detección de cabecera; está en la asignación de líneas físicas al bloque lógico.

Además, la spec base actual de `ing-block-reconstruction` codifica justo la suposición que el debugger ha refutado: “las líneas sin fecha inicial MUST adjuntarse al bloque previo”. Este cambio necesita delta de spec además del ajuste de parser (`openspec/specs/ing-block-reconstruction/spec.md`).

### Affected Areas
- `src/SauronSheet.Infrastructure/PDF/Parsers/IngBlockAssembler.cs` — punto central del bug; necesita dejar de adjuntar ciegamente toda línea sin fecha al bloque previo.
- `src/SauronSheet.Infrastructure/PDF/Parsers/IngBankPdfParser.cs` — probablemente solo ajuste menor de cableado/contrato si el ensamblado pasa a usar buffer ambiguo o helper de ancla.
- `src/SauronSheet.Infrastructure/PDF/Parsers/IngMonetaryExtractor.cs` — reutilizable para detectar una ancla fuerte (`fecha en primera columna + importe/saldo aislables`) sin introducir heurísticas nuevas de taxonomía.
- `tests/SauronSheet.Infrastructure.Tests/PDF/Parsers/IngBlockAssemblerTests.cs` — deben añadirse casos de líneas sin fecha que pasan al siguiente bloque anclado.
- `tests/SauronSheet.Infrastructure.Tests/PDF/Parsers/IngBankPdfParserBlockTests.cs` — deben cubrir nómina con ancla en medio y proteger las regresiones de repeated-page-header ya existentes.
- `tests/SauronSheet.Infrastructure.Tests/PDF/Parsers/IngControlledTaxonomyTests.cs` — conviene añadir una aserción de pipeline para confirmar que, tras reagrupar, la descripción contiene los fragmentos anterior/posterior correctos sin contaminar la fila previa.
- `openspec/specs/ing-block-reconstruction/spec.md` — requiere delta porque IBR-1 ya no puede afirmar que toda línea sin fecha pertenece al bloque anterior.

### Approaches
1. **Parche local post-parse** — mover texto “huérfano” desde la fila previa a la siguiente después de construir `RawTransactionRow`.
   - Pros: diff corto.
   - Cons: corrige demasiado tarde, mezcla responsabilidades y es fácil romper repeated-page-header o descripciones válidas.
   - Effort: Medio.

2. **Ensamblado anchor-aware con buffer ambiguo** — mantener el pipeline actual, pero rediseñar `IngBlockAssembler` para distinguir entre continuación real del bloque anterior y fragmento previo a una ancla fuerte.
   - Pros: cambio mínimo y seguro en la capa correcta; no toca Application/Domain; preserva `ProcessBlocks`, extractor monetario y detección de cabecera.
   - Cons: requiere una heurística explícita y tests de borde para no “robar” continuaciones legítimas.
   - Effort: Medio.

3. **Re-segmentación geométrica completa por coordenadas X/Y** — rehacer el agrupado usando layout visual de toda la página.
   - Pros: mayor fidelidad al PDF.
   - Cons: es bastante más grande que el bug, aumenta riesgo de review y no aporta una mejora proporcional para este caso concreto.
   - Effort: Alto.

### Recommendation
Recomiendo **Approach 2** como cambio de seguimiento SOLO del parser.

Rediseño mínimo seguro:
- Tratar como **ancla fuerte** una línea que ya cumple el contrato estructural mínimo: fecha en primera columna (`TryGetBlockStartDate(...)`) y par `importe/saldo` aislable en esa misma línea mediante `IngMonetaryExtractor.ExtractRightToLeft(...)`.
- Mientras el bloque actual aún no tiene importes/saldo aislables, las líneas sin fecha deben seguir yendo hacia atrás. Esto preserva el caso ya arreglado de continuación tras repeated-page-header y los bloques cuyo importe llega en una línea posterior.
- Cuando el bloque actual YA está “completo” y aparecen líneas sin fecha antes de otra ancla fuerte, esas líneas deben pasar a un **buffer ambiguo** en vez de adjuntarse inmediatamente al bloque anterior.
- Si después aparece una ancla fuerte nueva, ese buffer se antepone al nuevo bloque: `leading-description + anchor-line + trailing-description`.
- Si no aparece una ancla nueva y se llega a EOF, el buffer se reanexa al bloque actual para no perder texto.

Con esta estrategia, el patrón del usuario (`A` sin fecha + `B` ancla + `C` sin fecha) queda agrupado alrededor de `B`, mientras que la continuación tras repeated-page-header sigue perteneciendo al bloque anterior cuando ese bloque todavía no está completo.

### Risks
- Un bloque anterior ya “completo” podría tener texto legítimo tardío y el nuevo buffer podría reasignarlo hacia delante si la heurística no se limita bien a anclas fuertes.
- Si la noción de “bloque completo” se calcula mal, se puede romper la regresión ya cubierta de repeated-page-header.
- La taxonomía de nómina puede seguir siendo parcial después del reagrupado (`IngControlledTaxonomy` solo conoce `Nómina`/`Nomina`), así que este cambio no debe prometer resolver todo el etiquetado semántico.
- La spec IBR-1 actual quedará incoherente si no se acompaña con delta en OpenSpec.

### Ready for Proposal
Sí — el orquestador debe proponer un follow-up parser-only centrado en `IngBlockAssembler`, con delta de spec en `ing-block-reconstruction` y tests nuevos para: (1) nómina con línea ancla en medio, (2) línea previa sin fecha que se reasigna al siguiente bloque, y (3) regresión repeated-page-header que debe seguir pasando. Con presupuesto de 800 líneas, esto encaja en una sola PR si se mantiene acotado al parser y tests.
