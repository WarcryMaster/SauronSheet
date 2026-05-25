## Exploration: pdf-driven-category-import

### Current State
El import actual NO es PDF-driven de verdad. `IngBankPdfParser` intenta extraer categoría y subcategoría comparando el texto del PDF contra listas hardcodeadas (`KnownCategories` y `KnownSubCategories`). Cuando el PDF trae valores no contemplados, esos literales pueden perderse o quedarse en `null`, así que la fuente de verdad ya llega degradada al handler.

Después del parseo, `ImportTransactionsFromPdfCommandHandler` llama a `IBankCategoryResolutionService.ResolveAsync(...)`, que solo hace lookup: consulta `bank_category_translations`, busca coincidencia por nombre sobre categorías ya existentes del usuario y, si existe subcategoría, intenta encontrarla dentro de la categoría resuelta. No crea categorías ni subcategorías.

En persistencia, `transactions` ya guarda `bank_category`, `bank_subcategory`, `subcategory_id` y `category_source`, además de `category_id`. Eso ayuda porque el dato bruto ya existe en el modelo. El problema es que el pipeline sigue dependiendo de traducciones y categorías preexistentes, y el helper de UI (`TransactionCategoryDisplayHelper`) prioriza la categoría resuelta antes que el valor bruto del PDF.

También hay deriva arquitectónica: el dominio y la interfaz dicen que los system defaults ya no deben participar, pero el código de infraestructura aún conserva `IsSystemDefault`, `GetSystemDefaultsAsync()`, migraciones históricas de categorías globales y un `CreateCategoryCommandHandler` que sigue comprobando duplicados con `FindByNameAsync()` a través de todos los scopes.

### Affected Areas
- `src/SauronSheet.Infrastructure/PDF/Parsers/IngBankPdfParser.cs` — hoy extrae categoría/subcategoría con listas cerradas y heurística textual; es la principal debilidad frente al requisito de “raw PDF as source of truth”.
- `src/SauronSheet.Domain/ValueObjects/RawTransactionRow.cs` — ya soporta `Category` y `SubCategory`, pero hoy recibe valores posiblemente degradados por el parser.
- `src/SauronSheet.Application/Features/Transactions/Commands/ImportTransactionsFromPdfCommandHandler.cs` — orquesta parseo, deduplicación, resolución y persistencia; aquí cambiaría el flujo a get-or-add.
- `src/SauronSheet.Application/Services/BankCategoryResolutionService.cs` — hoy resuelve por traducción + match de nombre; tendría que evolucionar o ser sustituido por un servicio PDF-driven de lookup/create.
- `src/SauronSheet.Infrastructure/Persistence/SupabaseBankCategoryTranslationRepository.cs` — representa la ruta actual de traducción; con el nuevo modelo pasaría a ser legado o quedaría fuera del import PDF.
- `src/SauronSheet.Infrastructure/Persistence/SupabaseCategoryRepository.cs` — ya tiene `FindByNameAndUserAsync` y `AddAsync`, pero el lookup actual es exacto y sin clave normalizada persistida.
- `src/SauronSheet.Infrastructure/Persistence/SupabaseSubcategoryRepository.cs` — ya tiene `FindByNameAsync(userId, categoryId, name)` y `AddAsync`; mismo problema de deduplicación sin clave normalizada en base de datos.
- `src/SauronSheet.Domain/Entities/Category.cs` y `src/SauronSheet.Domain/Services/CategoryService.cs` — aún arrastran semántica de system defaults que choca con la nueva regla de negocio.
- `src/SauronSheet.Infrastructure/Persistence/SupabaseTransactionRepository.cs` y `src/SauronSheet.Domain/Entities/Transaction.cs` — ya soportan persistir bruto + IDs asociados, así que el modelo base sirve.
- `src/SauronSheet.Application/Features/Transactions/Queries/GetTransactionsQueryHandler.cs` — expone ambos mundos (resuelto y bruto) hacia la UI.
- `src/SauronSheet.Application/Features/Transactions/Queries/GetRecentTransactionsQueryHandler.cs` — mismo impacto en lecturas recientes.
- `src/SauronSheet.Application/Features/Transactions/Queries/SearchTransactionsQueryHandler.cs` — mismo impacto en búsquedas.
- `src/SauronSheet.Frontend/Helpers/TransactionCategoryDisplayHelper.cs` — hoy prioriza `CategoryName/SubcategoryName`; para importadas debe priorizar `BankCategory/BankSubcategory`.
- `src/SauronSheet.Frontend/Pages/Transactions/Index.cshtml.cs` y `src/SauronSheet.Frontend/Pages/Transactions/Search.cshtml.cs` — consumen los DTOs donde cambiará la semántica visual.
- `src/SauronSheet.Infrastructure/Persistence/Migrations/007_SystemCategoriesGlobalScope.sql` y `008_RevertSystemCategoriesGlobalScope.sql` — muestran legado de categorías predefinidas/globales que hay que neutralizar para que no entren en import.
- `src/SauronSheet.Infrastructure/Persistence/Migrations/010_EnableRLSOnSubcategoriesAndBankTranslations.sql` — introduce restricciones/hipótesis de RLS que deben revisarse antes de cualquier get-or-add sobre subcategorías.

### Approaches
1. **Camino evolutivo mínimo** — Mantener el modelo actual y reemplazar solo la resolución por un get-or-add en Application/Infrastructure.
   - Pros: Reutiliza `Transaction`, repositorios y DTOs actuales; menor tamaño de cambio; encaja mejor en chained PRs.
   - Cons: Sigue mezclando legado de traducciones/system defaults con el nuevo flujo; la normalización quedaría probablemente en aplicación e in-memory; riesgo de duplicados por carrera si dos imports crean la misma categoría a la vez.
   - Effort: Medio

2. **Camino arquitectónico limpio** — Introducir un servicio explícito de clasificación PDF-driven (`PdfCategoryCatalogService` o equivalente) con clave normalizada estable para categorías/subcategorías, dejando la ruta de traducción fuera del import PDF.
   - Pros: Alinea el diseño con la regla de negocio; separa claramente raw values, normalization key y display values; facilita backfill y reduce ambigüedad futura.
   - Cons: Requiere tocar más capas, probablemente añadir columnas/índices únicos o una estrategia RPC/SQL para get-or-add seguro; mayor superficie de migración.
   - Effort: Alto

### Recommendation
Recomiendo un enfoque híbrido: implementar primero el **camino evolutivo**, pero con DOS decisiones del camino limpio desde el principio: (1) separar el servicio actual de traducción del nuevo flujo PDF-driven y (2) introducir una clave normalizada persistible/consultable para evitar deduplicación frágil. En otras palabras: cambio incremental en entrega, pero diseño correcto en el núcleo. Si se intenta hacer solo lookup in-memory + insert sin una garantía de unicidad, se construirá una casa sobre arena.

La secuencia recomendada sería: corregir parser para preservar literales del PDF, introducir servicio get-or-add PDF-driven, excluir explícitamente categorías predefinidas del lookup/import, adaptar la UI para priorizar bruto y decidir después si el backfill de históricos entra en este cambio o en uno separado.

### Risks
- **Pérdida de literal bruto en parser ING**: mientras `IngBankPdfParser` siga dependiendo de listas hardcodeadas, el requisito de “source of truth” NO se cumple.
- **Condiciones de carrera en get-or-add**: con Supabase/Postgrest C# el patrón leer→comparar→insertar sin constraint único puede crear duplicados.
- **Normalización inconsistente**: si la clave normalizada vive solo en C#, cualquier cambio futuro en la función de normalización romperá deduplicación y backfill.
- **Legado de system defaults**: hay restos de categorías globales/predefinidas en código y migraciones; si no se filtran o limpian, el import puede seguir enlazando contra defaults.
- **Backfill ambiguo**: transacciones históricas ya tienen `bank_category`/`bank_subcategory`, pero habrá que decidir si se recalculan IDs, si se respetan overrides manuales y cómo tratar categorías antiguas.
- **UI con doble semántica**: priorizar bruto para importadas puede entrar en conflicto con recategorizaciones manuales si no se define claramente cuándo gana `CategorySource.UserOverride`.
- **Restricciones Postgrest/Supabase**: no hay OR en `.Where()`, no se admiten method calls dentro de lambdas y el matching case/accent-insensitive suele acabar en memoria salvo que se mueva a SQL/RPC.

### Ready for Proposal
Sí — con una aclaración para el orquestador: la propuesta debe decidir explícitamente si este cambio incluye solo el nuevo flujo para imports futuros o también un backfill de históricos, y debe reservar un slice específico para parser + normalización porque ahí está el verdadero cuello de botella del requisito.
