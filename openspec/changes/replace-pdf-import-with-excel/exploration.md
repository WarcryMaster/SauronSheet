## Exploration: replace-pdf-import-with-excel

### Current State
- `/transactions/upload` es una Razor Page. `Upload.cshtml` solo acepta `.pdf`; `Upload.cshtml.cs` valida PDF/10MB y lanza `ImportTransactionsFromPdfCommand`.
- No hay endpoint API ni validators dedicados para importación; la validación está inline en la PageModel y en el handler.
- `ImportTransactionsFromPdfCommandHandler` asegura el perfil de usuario, exige extensión `.pdf`, delega en `IPdfParser` (`AdaptivePdfParser` → `IngBankPdfParser` o `GenericBankPdfParser`), valida solo `Date/Description/Amount`, normaliza importes, deduplica por `user + date + amount + description`, resuelve/crea categorías y persiste `Transaction` + `ImportBatch` en `pdf_imports`.
- Las transacciones importadas se exponen por nombre de fichero mediante `ImportedFrom`; la lista, el filtro de fuentes y el dashboard dependen solo del filename, no del tipo de archivo.
- El comportamiento de negocio actual NO persiste `Balance` ni `Comment`; ambos viven en `RawTransactionRow`, pero el handler los ignora. Para importar hoy solo son obligatorios `Date`, `Description` y `Amount`; `Category/SubCategory` enriquecen la transacción.
- Los Excel de muestra en `src/SauronSheet.Infrastructure/Excel/` son `.xls` legacy, hoja `Movimientos`, filas 1-3 de metadatos, fila 4 de cabecera y fila 5+ de datos con columnas `F. VALOR | CATEGORÍA | SUBCATEGORÍA | DESCRIPCIÓN | COMENTARIO | IMPORTE | SALDO`. En las seis muestras, categoría/subcategoría/descripción siempre vienen informadas; `COMENTARIO` casi siempre está vacío, pero existe al menos una fila con texto.

### Affected Areas
- `src/SauronSheet.Frontend/Pages/Transactions/Upload.cshtml` — input file, copy, `accept`, drag&drop y mensajes siguen siendo PDF-only.
- `src/SauronSheet.Frontend/Pages/Transactions/Upload.cshtml.cs` — validación de extensión/tamaño, dispatch del comando y métricas `app.pdf.import.*`.
- `src/SauronSheet.Frontend/Pages/Transactions/Index.cshtml` — botones y empty state todavía dicen `Upload PDF` / `Import PDF`.
- `src/SauronSheet.Frontend/Pages/Dashboard.cshtml` — empty state enlaza a `Upload PDF`.
- `src/SauronSheet.Frontend/Pages/Shared/_Layout.cshtml` — navegación global enlaza a `/transactions/upload` con copy genérico heredado del flujo PDF.
- `src/SauronSheet.Application/Features/Transactions/Commands/ImportTransactionsFromPdfCommand.cs` — contrato de aplicación hardcodeado a PDF.
- `src/SauronSheet.Application/Features/Transactions/Commands/ImportTransactionsFromPdfCommandHandler.cs` — corazón del comportamiento de negocio reutilizable si se sustituye solo la extracción.
- `src/SauronSheet.Domain/Services/IPdfParser.cs` — contrato de parsing demasiado específico para un reemplazo limpio.
- `src/SauronSheet.Domain/ValueObjects/RawTransactionRow.cs` — contrato intermedio válido para Excel porque ya soporta fecha/categoría/subcategoría/descripción/comentario/importe/saldo.
- `src/SauronSheet.Infrastructure/PDF/Parsers/AdaptivePdfParser.cs` — detección y dispatch actuales del flujo PDF; quedaría legado o sustituido.
- `src/SauronSheet.Infrastructure/PDF/Parsers/IngBankPdfParser.cs` y `GenericBankPdfParser.cs` — parsers actuales; útiles como referencia de normalización y cobertura, pero no reutilizables directamente para Excel.
- `src/SauronSheet.Infrastructure/Excel/*.xls` — contrato real de entrada para el parser Excel.
- `src/SauronSheet.Application/Services/BankCategoryResolutionService.cs` — ya implementa `ResolveOrCreateAsync`; puede mantenerse para conservar el comportamiento de categorías.
- `src/SauronSheet.Domain/Entities/Transaction.cs` y `src/SauronSheet.Infrastructure/Persistence/SupabaseTransactionRepository.cs` — persistencia actual de `ImportedFrom`, `BankCategory`, `BankSubcategory`, `CategoryId`, `SubcategoryId` y `CategorySource`.
- `src/SauronSheet.Domain/Entities/ImportBatch.cs`, `src/SauronSheet.Domain/Repositories/IPdfImportRepository.cs`, `src/SauronSheet.Infrastructure/Persistence/SupabasePdfImportRepository.cs`, `src/SauronSheet.Infrastructure/Persistence/Migrations/004_CreatePdfImportsTable.sql` — metadata funcionalmente reutilizable, pero con naming PDF-specific.
- `src/SauronSheet.Application/Features/Transactions/Queries/GetDistinctImportedSourcesQuery*.cs`, `src/SauronSheet.Domain/Specifications/TransactionByImportedFromSpecification.cs`, `src/SauronSheet.Frontend/Pages/Transactions/Index.cshtml.cs` — filtro/listado de fuentes importadas basado en filename.
- `tests/SauronSheet.Application.Tests/Features/Transactions/Commands/ImportTransactionsFromPdfCommandTests.cs` — cobertura del flujo de negocio (válidos, duplicados, errores, trimming, resolución).
- `tests/SauronSheet.Infrastructure.Tests/PDF/Parsers/AdaptivePdfParserDetectionTests.cs` y `tests/SauronSheet.Infrastructure.Tests/PDF/Parsers/IngBankPdfParserBlockTests.cs` — cobertura de parsing PDF actual; habría que añadir una batería equivalente para Excel.
- `tests/SauronSheet.Application.Tests/Features/Transactions/Queries/GetDistinctImportedSourcesQueryTests.cs` y `tests/SauronSheet.Domain.Tests/Specifications/TransactionByImportedFromSpecificationTests.cs` — garantizan que el comportamiento por fuente sigue estable.

### Approaches
1. **Sustitución mínima dentro del flujo PDF** — mantener la ruta y gran parte del naming actual, pero cambiar la carga/validación para leer Excel y mapearlo a `RawTransactionRow`.
   - Pros: reutiliza casi intacta la lógica de negocio existente; menor diff inicial.
   - Cons: deja deuda semántica (`Pdf`, `pdf_imports`, métricas `app.pdf.import.*`, mensajes de error); arquitectura confusa para el siguiente cambio.
   - Effort: Medium

2. **Refactor a flujo neutro de extractos + parser Excel** — introducir un contrato neutro de importación/parsing y reutilizar intactas las reglas de deduplicación, resolución y persistencia.
   - Pros: diseño correcto, mismo comportamiento funcional, menos deuda futura, mejor base para chained PRs y futuros formatos.
   - Cons: toca más ficheros transversales y obliga a decidir si se renombran también repositorio/métricas/tablas o se deja un adapter temporal.
   - Effort: Medium

3. **Soporte estricto solo al Excel exportado por ING** — parser basado en la estructura exacta observada en las muestras (`Movimientos`, cabecera en fila 4, datos desde fila 5).
   - Pros: menor ambigüedad; contrato sustentado por ficheros reales; reduce riesgo de interpretar hojas manuales erróneas.
   - Cons: no cubre bien Excel manual ni variantes de otros bancos; probablemente obliga a aclarar si `.xlsx` entra o no.
   - Effort: Medium

### Recommendation
Recomiendo **combinar 2 + 3**: refactor pequeño a flujo neutro de extractos, pero con un primer slice que soporte SOLO el contrato real de los `.xls` de muestra. Así se mantiene exactamente el comportamiento de negocio actual — deduplicación, `ResolveOrCreateAsync`, persistencia de `BankCategory/BankSubcategory`, `ImportedFrom` por filename y resumen imported/skipped/errors — sin quedarse atrapado en naming PDF-specific.

La secuencia razonable para chained PRs es: **(1)** parser Excel + tests sobre muestras reales, **(2)** neutralización de comando/copy/métricas/DI, **(3)** limpieza de restos PDF y ajuste final de UX. Si se intenta hacer solo un “swap rápido” dentro de tipos `Pdf*`, funcionará, pero la base quedará mal nombrada y más cara de mantener.

### Risks
- No existe hoy ninguna dependencia de lectura Excel en `src/SauronSheet.Infrastructure/SauronSheet.Infrastructure.csproj`; habrá que añadir una librería con soporte real para `.xls`.
- Los ficheros son `.xls` legacy; la elección del lector debe manejar bien code pages y acentos.
- `COMENTARIO` y `SALDO` existen en las muestras, pero el comportamiento actual los descarta. Si el usuario espera conservarlos, ya NO sería “mismo comportamiento”.
- `pdf_imports`, `IPdfImportRepository`, `ImportTransactionsFromPdfCommand` y `app.pdf.import.*` siguen siendo PDF-specific; un reemplazo parcial dejaría deuda semántica.
- La creación manual de hojas Excel solo es realista con plantilla fija y validación estricta; una hoja “libre” no es segura.
- No he encontrado tests frontend del upload actual; la regresión de copy/validación quedaría menos protegida que la lógica de aplicación.
- `GenericBankPdfParser` todavía contiene un `Console.WriteLine`, lo que revela deuda de observabilidad en esta zona del código.

### Qué necesito del usuario
- **Confirmaciones / decisiones obligatorias**
  - Confirmar formatos aceptados: ¿solo `.xls` como las muestras o también `.xlsx`?
  - Confirmar si `/transactions/upload` pasa a ser Excel-only o si debe convivir temporalmente con PDF.
  - Confirmar si `COMENTARIO` y `SALDO` deben seguir ignorándose para mantener el comportamiento actual.
  - Confirmar si `ImportedFrom` debe seguir guardando el filename original tal cual.
  - Confirmar si se quiere soportar Excel manual y, en ese caso, si será **solo una plantilla exacta** o una importación tolerante por nombre de columnas.
  - Aportar, si existen, muestras problemáticas: subcategoría vacía, columnas reordenadas, otra hoja distinta de `Movimientos`, o exportaciones `.xlsx`.
- **Información nice-to-have**
  - Un fichero real grande (muchas filas) para estimar rendimiento y tamaño máximo razonable.
  - Un ejemplo con más variedad de casos (transferencias, ingresos, compras, comentarios, valores vacíos).
  - Confirmación de si el alcance es solo ING o si hay que pensar ya en otros bancos.
- **Lo que NO hace falta pedir porque el código o las muestras ya lo definen**
  - La regla de duplicados actual: `user + date + amount + description`.
  - Los campos realmente obligatorios para importar hoy: `Date`, `Description`, `Amount`.
  - El comportamiento de categorías: usar raw bank category/subcategory para `ResolveOrCreateAsync` y persistirlos en `BankCategory/BankSubcategory`.
  - El contrato visible de resultado: imported/skipped/errores por fila + filtrado por `ImportedFrom`.
  - La estructura base de las muestras ya entregadas: hoja `Movimientos`, cabecera en fila 4, datos desde fila 5.

### Ready for Proposal
Sí — el orquestador ya puede preparar propuesta, pero debe explicitar tres decisiones antes de aplicar: **(1)** `.xls` solo vs `.xlsx` también, **(2)** si `COMENTARIO/SALDO` siguen descartados adrede, y **(3)** si el Excel manual será plantilla cerrada o quedará fuera de alcance.

### Envelope
- status: success
- executive_summary: He recorrido el flujo actual de importación extremo a extremo y he validado las seis muestras Excel reales. Sustituir PDF por Excel es viable SIN cambiar la lógica de negocio central, pero hay naming PDF-specific en UI, comando, métricas y metadata que conviene neutralizar en slices encadenados.
- artifacts:
  - Engram `sdd/replace-pdf-import-with-excel/explore`
  - `openspec/changes/replace-pdf-import-with-excel/exploration.md`
- next_recommended: sdd-propose
- risks: Soporte `.xls` legacy, deuda semántica PDF-specific, definición de `COMENTARIO/SALDO`, y ambigüedad sobre Excel manual.
- skill_resolution: none
