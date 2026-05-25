# Delta para category-resolution

> Cobertura de los dos warnings del archive report de `fix-transaction-category-retrieval`.
> Añade el seam de repositorio para CR-2e (verificable a nivel de infraestructura)
> y el contrato de batch para DT-1 (`GetTransactionsQueryHandler`).

---

## MODIFIED Requirements

### Requirement: CR-2

ICategoryResolutionService MUST aceptar (userId, bankCategory, bankSubcategory) y retornar
(CategoryId?, SubcategoryId?, CategorySource). MUST NOT crear nada. La búsqueda de traducción
MUST evaluar la coincidencia exacta (bank_category + bank_subcategory) ANTES del fallback
genérico (bank_category, null). El repositorio de traducciones MUST exponer seams
`protected internal virtual` para que los tests de infraestructura puedan verificar el orden
de ejecución de las queries sin depender del cliente Supabase real.
(Previously: sin contrato de seam de repositorio; CR-2e no era verificable al nivel de infraestructura)

#### CR-2a: Match vía traducción

- GIVEN bank_category_translations con (bank_category="Aliment.", resolved_category_name="Alimentación") Y Category("Alimentación") existe
- WHEN Resolve(userId, "Aliment.", null)
- THEN resolved_category_name="Alimentación", Category("Alimentación") encontrada por nombre, source=AutoMatched

#### CR-2b: Match directo (case-insensitive)

- GIVEN bank_category="compras" Y Category("Compras")
- WHEN Resolve(userId, "compras", null)
- THEN CategoryId=id("Compras"), source=AutoMatched

#### CR-2c: Sin match → RawOnly

- GIVEN bank_category="ING Direct" sin traducción ni categoría del usuario
- WHEN ResolutionService.Resolve(userId, "ING Direct", null)
- THEN CategoryId=null, SubcategoryId=null, source=RawOnly

#### CR-2d: Subcategoría anidada

- GIVEN Category("Compras") con Subcategory("Ropa y complementos")
- WHEN ResolutionService.Resolve(userId, "Compras", "Ropa y complementos")
- THEN CategoryId Y SubcategoryId asignados, source=AutoMatched

#### CR-2e: Traducción exacta prevalece sobre genérica (regresión)

- GIVEN translations: fila-A (bank_category="Compras", bank_subcategory="Ropa", resolved="Moda") Y fila-B (bank_category="Compras", bank_subcategory=null, resolved="General")
- WHEN Resolve(userId, "Compras", "Ropa")
- THEN resolved_category_name="Moda" (exacta gana; fila-B ignorada)

#### CR-2e-infra: Seam de repositorio — exacta ejecutada antes que genérica

- GIVEN `TestableSupabaseBankCategoryTranslationRepository` con fila-A exacta (category="Compras", subcategory="Ropa") y fila-B genérica (category="Compras", subcategory=null)
- WHEN `GetTranslationAsync(userId, "Compras", "Ropa")` es invocado
- THEN `ExecuteExactMatchQueryAsync` es llamado ANTES que `ExecuteGenericMatchQueryAsync`
- AND el resultado devuelve fila-A (exacta)

#### CR-2e-infra-fallback: Seam de repositorio — fallback genérico cuando no hay exacta

- GIVEN `TestableSupabaseBankCategoryTranslationRepository` con solo fila-B genérica (category="Compras", subcategory=null)
- WHEN `GetTranslationAsync(userId, "Compras", "Ropa")` es invocado
- THEN `ExecuteExactMatchQueryAsync` devuelve vacío
- AND `ExecuteGenericMatchQueryAsync` devuelve fila-B

---

### Requirement: DT-1

TransactionDto MUST añadir BankCategory, BankSubcategory, SubcategoryId, SubcategoryName,
CategorySource. Los query handlers MUST poblar SubcategoryName cuando SubcategoryId != null;
MUST NOT devolver null en ese caso. `GetTransactionsQueryHandler` MUST resolver los nombres de
categoría mediante una única llamada batch a `GetByUserIdAsync`; MUST NOT llamar a
`GetByIdAsync` por categoría individual durante la resolución de nombres.
(Previously: sin contrato de batch para `GetTransactionsQueryHandler`; handler usaba N+1 sin escenario verificable)

#### DT-1: DTO completo

- GIVEN Transaction con bankCategory="Compras", source=AutoMatched
- WHEN se mapea a TransactionDto
- THEN BankCategory="Compras" Y CategorySource=AutoMatched

#### DT-1b: SubcategoryName poblada en lectura (regresión)

- GIVEN Transaction con SubcategoryId=X Y Subcategory(id=X, name="Ropa") en repositorio
- WHEN un query handler construye TransactionDto
- THEN SubcategoryName="Ropa" (NOT null)

#### DT-1c: SubcategoryName null cuando sin subcategoría

- GIVEN Transaction con SubcategoryId=null
- WHEN un query handler construye TransactionDto
- THEN SubcategoryName=null

#### DT-1d: GetTransactionsQueryHandler resuelve categorías en batch

- GIVEN `GetTransactionsQuery` para userId con N transacciones de M categorías distintas (M > 1)
- WHEN `Handle` es ejecutado
- THEN `ICategoryRepository.GetByUserIdAsync(userId)` es llamado exactamente una vez
- AND `ICategoryRepository.GetByIdAsync` no es llamado para resolución de nombres de categoría

---

## REMOVED Requirements

_(ninguno)_
